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

        public void SetLogicHandler(LogicHandler logicHandler)
        {
            _logicHandler = logicHandler;
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
                            Console.WriteLine($"Received message: {message}");
                            try
                            {
                                JsonDocument json = JsonDocument.Parse(message);
                                string type = json.RootElement.GetProperty("type").GetString() ?? "error";
                                await HandleMessage(type, json.RootElement.ToString());
                            } catch (Exception ex)
                            {
                                Console.WriteLine($"JSON error: {ex.Message}");
                            }
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocket 1 error: {ex.Message}");
                    ConnectedSockets.Remove(socket);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket 2 error: {ex.Message}");
                }
            }
        }

        private async Task HandleMessage(string type, string message)
        {
            //switch (type)
            //{
            //    case "get_interfaces":
            //        InterfaceHandler.Instance.SendConnections();
            //        break;

            //    case "get_downloads":
            //        DownloadHandler.Instance.SendDownloads();
            //        break;

            //    case "memory_download":
            //        MemoryDownload data = JsonSerializer.Deserialize<MemoryDownload>(message) ?? throw new Exception("Could not deserialize MemoryDownload");
            //        DownloadHandler.Instance.AddDownload(data);
            //        break;

            //    default:
            //        Debug.WriteLine("Unknown Message Type: " + type);
            //        break;
            //}
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