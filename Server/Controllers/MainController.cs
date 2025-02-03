using Microsoft.AspNetCore.Mvc;
using Server.Models;
using System.Net.WebSockets;
using System.Text;

namespace Server.Controllers
{
    public class MainController(ILogger<MainController> logger) : ControllerBase
    {
        private const double MaxWorkDelay = 4.0;
        private const double MinWorkDelay = 1.0;
        private const int MaxByteLength = 1024;
        private const string WelcomeMessage = "Welcome";

        private static readonly List<WebSocket> _clients = [];

        [HttpPost("/server/ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ServerPing()
        {
            BroadcastMessage("Pong");
            return Ok();
        }

        [HttpPost("/work/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult WorkStart([FromBody] WorkModel workModel)
        {
            BroadcastMessage($"WorkStarted(Id:{workModel.Id})");
            Task.Run(async () =>
            {
                await Task.Delay(GetWorkDuration());
                BroadcastMessage($"WorkCompleted(Id:{workModel.Id})");
            }).ConfigureAwait(false);
            return Ok();
        }

        [Route("/messages")]
        public async Task Messages()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await SendMessage(webSocket, WelcomeMessage);

                lock (_clients)
                    _clients.Add(webSocket);

                try
                {
                    await WaitForClose(webSocket);
                }
                catch (ObjectDisposedException e)
                {
                    logger.LogWarning("Connection ({socket}) was closed abruptly", webSocket);
                }
                finally
                {
                    lock (_clients)
                        _clients.Remove(webSocket);
                    logger.LogDebug("\x1B[42mRemoved client\x1B[49m");
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private static TimeSpan GetWorkDuration()
        {
            var random = new Random();
            var duration = MinWorkDelay + (random.NextDouble() * (MaxWorkDelay - MinWorkDelay));
            return TimeSpan.FromSeconds(duration);
        }

        private static void BroadcastMessage(string message)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    SendMessage(client, message).ConfigureAwait(false);
                }
            }
        }

        private static async Task SendMessage(WebSocket websocket, string message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var messageArray = new ArraySegment<byte>(messageBytes, 0, messageBytes.Length);
            await websocket.SendAsync(messageArray, messageType, endOfMessage, CancellationToken.None);
        }

        private async Task WaitForClose(WebSocket webSocket)
        {
            while (true)
            {
                var buffer = new byte[MaxByteLength];
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.CloseStatus.HasValue)
                {
                    await webSocket.CloseAsync(
                        receiveResult.CloseStatus.Value,
                        receiveResult.CloseStatusDescription,
                        CancellationToken.None);
                    break;
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer);
                    logger.LogDebug("\u001b[42mUnexpected message: {message}\u001b[49m", message);
                }
            }
        }
    }
}
