<style>
    string-quote
    {
        color: green;
    }
    endpoint
    {
        background: #333;
        color: white;
        font-family: monospace;
    }
</style>

# Web API Demo
Short demonstration on C# Web API (REST + Web Sockets)

## Requirements
- Visual Studio 2022
- .Net 8.0
- WebSocket-compatible browser

# Running the Demo

1. Open WebApiDemo.sln in Visual Studio
1. Build and Run Server.csproj
   - You may need to accept/ignore HTTPS Certificate errors
1. The server runs as a console application
   - This can simply be minimized unless you wish to watch the logs flow
1. The client is a single HTML page which should automatically be launched for you
   - If the client didn't launch, navigate to https://localhost:58074 using your favorite browser
   - The url can be configured in Properties/launchSettings.json
1. Closing the browser will also terminate the debug session

# Server Specifications
There are 3 endpoints: <endpoint>/server/ping</endpoint>, <endpoint>/work/start</endpoint>, <endpoint>/messages</endpoint>

## /server/ping
This is an HTTP POST endpoint. The server will respond with Status Code 200 and an empty response body.

## /work/start
This is an HTTP POST endpoint and it's expecting a body with the following format:
```
{
    "Id":"any string"
}
```
The server will respond with Status Code 200 and an empty response body.

## /messages
This is a websocket endpoint. The server will send string messages to the client. The server does not expect to receive any message from the client other than closing the connection.

# Client Specifications
The client can perform the following:

- Connect to <endpoint>/messages</endpoint> via websocket
- Send an HTTP POST request to <endpoint>/server/ping</endpoint>
- Send an HTTP POST request to <endpoint>/work/start</endpoint> with the expected json body
- Log all events

# Client-Server Interaction

- When a client connects to <endpoint>/messages</endpoint>, the server immediately sends a <string-quote>"Welcome"</string-quote> string message
- When the server receives a request to <endpoint>/server/ping</endpoint>, all connected clients will receive a <string-quote>"Pong"</string-quote> string message. The request to <endpoint>/server/ping</endpoint> can come from any where, not just from a connected client.
- When the server receives a request to <endpoint>/work/start</endpoint>, all connected clients will receive a <string-quote>"WorkStarted(Id:workId)"</string-quote> string message where *workId* is the <string-quote>"Id"</string-quote> in the request body. The request to <endpoint>/work/start</endpoint> can come from any where, not just from a connected client. After some time (a random duration between 1 and 4 seconds), the server will send a <string-quote>"WorkCompleted(Id:workId)"</string-quote> string message, where *workId* is the same as before, to all connected clients.
- Messages are sent immediately, meaning they are not queued.

# Known Errors
- There is no HTTPS or SSL certificate and this will show up as an exception in the logs

# References
- C# RESTful Web API - https://www.youtube.com/playlist?list=PL3ewn8T-zRWgO-GAdXjVRh-6thRog6ddg (Part 1 to 14)
- MSDN WebSockets supports in .Net Core - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-9.0
- JavaScript POST Request - https://www.freecodecamp.org/news/javascript-post-request-how-to-send-an-http-post-request-in-js/
- Others - Microsoft Learn, ChatGPT, StackOverflow, Medium