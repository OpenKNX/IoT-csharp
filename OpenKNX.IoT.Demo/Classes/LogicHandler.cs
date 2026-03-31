namespace OpenKNX.IoT.Demo.Classes
{
    public class LogicHandler
    {
        private KnxIotDevice _device;
        private WebsocketHandler _websocketHandler;

        public LogicHandler(WebsocketHandler websocket, KnxIotDevice device)
        {
            _websocketHandler = websocket;
            _device = device;
            _device.GroupMessageReceived += _device_GroupMessageReceived;
        }

        private bool _channel0State = false;
        private bool _channel1State = false;

        private void _device_GroupMessageReceived(object? sender, IoT.Models.GroupMessageEvent e)
        {
            switch(e.Href)
            {
                case "/p/lsab/0/soo":
                    {
                        bool oldState = _channel0State;
                        _channel0State = (bool)e.Data;
                        if(oldState != _channel0State)
                        {
                            string message = $"data={{\"type\":\"actuator\", \"channel\":0,\"state\":{_channel0State.ToString().ToLower()}}}";
                            _ = _websocketHandler.SendBroadcast(message);
                            _device.SendGroupMessage("/p/lsab/0/ioo", _channel0State);
                        }
                        break;
                    }

                case "/p/lsab/1/soo":
                    {
                        bool oldState = _channel1State;
                        _channel1State = (bool)e.Data;
                        if (oldState != _channel1State)
                        {
                            string message = $"data={{\"type\":\"actuator\", \"channel\":1,\"state\":{_channel1State.ToString().ToLower()}}}";
                            _ = _websocketHandler.SendBroadcast(message);
                            _device.SendGroupMessage("/p/lsab/1/ioo", _channel1State);
                        }
                        break;
                    }
            }

            //string message = "groupmessage=" + System.Text.Json.JsonSerializer.Serialize(e);
            //_ = _websocketHandler.SendBroadcast(message);
        }
    }
}
