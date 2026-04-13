using OpenKNX.IoT;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenKNX.IoT.Demo.Classes
{
    public class WebsocketHandler
    {
        public List<WebSocket> ConnectedSockets { get; } = new List<WebSocket>();

        private LogicHandler? _logicHandler;
        private ILogger? _logger;

        public void SetLogicHandler(LogicHandler logicHandler)
        {
            _logicHandler = logicHandler;
        }

        public void SetLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebsocketHandler>();
        }

        public async Task HandleWebSocket(WebSocket socket)
        {
            ConnectedSockets.Add(socket);
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketHandler", CancellationToken.None);
                        ConnectedSockets.Remove(socket);
                    }
                    else
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                            var parts = message.Split("=");
                            _logger?.LogInformation($"Received message: {parts[0]}");
                            if (parts[0] != "data")
                            {
                                _logger?.LogError("Unknown message type: " + parts[0]);
                                return;
                            }
                            try
                            {
                                JsonDocument json = JsonDocument.Parse(parts[1]);
                                string type = json.RootElement.GetProperty("type").GetString() ?? "error";
                                await HandleMessage(type, json);
                            } catch (Exception ex)
                            {
                                _logger?.LogError($"JSON error: {ex.Message}");
                            }
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    _logger?.LogError(ex, $"WebSocket 1 error: {ex.Message}");
                    ConnectedSockets.Remove(socket);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"WebSocket 2 error: {ex.Message}");
                }
            }
        }

        private async Task HandleMessage(string type, JsonDocument json)
        {
            switch (type)
            {
                case "switch":
                    int channel = json.RootElement.GetProperty("channel").GetInt32();
                    bool state = json.RootElement.GetProperty("state").GetBoolean();
                    _logicHandler?.SetChannelState(channel, state);
                    break;

                default:
                    _logger?.LogError("Unknown Message Type: " + type);
                    break;
            }
        }

        public async Task SendBroadcast(string message)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            var tasks = ConnectedSockets.Select(socket =>
            {
                if (socket.State == WebSocketState.Open)
                {
                    return socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    ConnectedSockets.Remove(socket);
                    return Task.CompletedTask;
                }
            }).ToArray();
            await Task.WhenAll(tasks);
        }
    }
}