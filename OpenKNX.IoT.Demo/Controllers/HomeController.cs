using Microsoft.AspNetCore.Mvc;
using OpenKNX.IoT.Demo.Classes;
using OpenKNX.IoT.Demo.Models;
using OpenKNX.IoT.Models;
using System.Diagnostics;
using System.Net.WebSockets;

namespace OpenKNX.IoT.Demo.Controllers
{
    public class HomeController : Controller
    {
        private KnxIotDevice _device;
        private WebsocketHandler _websocketHandler;

        public HomeController(KnxIotDevice device, WebsocketHandler websocketHandler)
        {
            _device = device;
            _websocketHandler = websocketHandler;
        }

        public IActionResult Index()
        {
            DeviceInfo info = _device.GetDeviceInfo();
            ViewData["serial"] = info.SerialNumber;
            ViewData["physical_address"] = info.PhysicalAddress;
            ViewData["installation_id"] = info.InstallationId;
            ViewData["host"] = info.Hostname;
            ViewData["lsm"] = info.LoadStateMachine;
            ViewData["progmode"] = info.ProgMode;
            string qrCode = $"KNX:S:{info.SerialNumber.ToUpper()};P:{info.Password.ToUpper()}";
            ViewData["qr-code-plain"] = qrCode;
            ViewData["qr-code-url"] = "https://api.qrserver.com/v1/create-qr-code/?data=" + qrCode.Replace(":", "%3A");
            return View();
        }

        public IActionResult Tables()
        {
            ViewData["groupobjects"] = _device.GetGroupObjectTableInfo();
            ViewData["publisher"] = _device.GetPublisherTableInfo();
            ViewData["recipient"] = _device.GetRecipientTableInfo();
            ViewData["auth"] = _device.GetAuthenticationTableInfo();
            ViewData["parameter"] = _device.GetParameterTableInfo();
            return View();
        }

        public async Task<IActionResult> Websocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                await _websocketHandler.HandleWebSocket(socket);

                return Ok();
            }
            else
            {
                return BadRequest("This endpoint only accepts WebSocket requests.");
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
