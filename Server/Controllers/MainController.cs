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

        private static readonly Queue<string> _messages = new Queue<string>();

        [HttpPost("/server/ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ServerPing()
        {
            logger.LogDebug("ServerPing");
            AddMessageToQueue("Pong");
            return Ok();
        }

        [HttpPost("/work/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult WorkStart([FromBody] WorkModel workModel)
        {
            logger.LogDebug("WorkStart({Id})", workModel.Id);
            AddMessageToQueue($"WorkStarted(Id:{workModel.Id})");
            Task.Run(async () =>
            {
                await Task.Delay(GetWorkDuration());
                AddMessageToQueue($"WorkCompleted(Id:{workModel.Id})");
            }).ConfigureAwait(false);
            return Ok();
        }

        [Route("/messages")]
        public async Task Messages()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await SendMessage(webSocket, "Welcome");
                await RelayMessages(webSocket);
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

        private static void AddMessageToQueue(string message)
        {
            lock (_messages)
            {
                _messages.Enqueue(message);
            }
        }

        private static string? GetMessageFromQueue()
        {
            lock (_messages)
            {
                return _messages.Count > 0 ? _messages.Dequeue() : null;
            }
        }

        private static async Task SendMessage(WebSocket websocket, string message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var messageArray = new ArraySegment<byte>(messageBytes, 0, messageBytes.Length);
            await websocket.SendAsync(messageArray, messageType, endOfMessage, CancellationToken.None);
        }

        private static async Task RelayMessages(WebSocket webSocket)
        {
            while (true)
            {
                var buffer = new byte[MaxByteLength];
                var receiveTask = webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!receiveTask.IsCompleted)
                {
                    var message = GetMessageFromQueue();
                    if (message is not null)
                        await SendMessage(webSocket, message);
                }

                var receiveResult = receiveTask.Result;
                if (receiveResult.CloseStatus.HasValue)
                {
                    await webSocket.CloseAsync(
                        receiveResult.CloseStatus.Value,
                        receiveResult.CloseStatusDescription,
                        CancellationToken.None);
                    break;
                }
            }
        }
    }
}
