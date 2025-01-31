using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Herta.Models.Enums.WebsocketEnum;
using Herta.Utils.Logger;
using Herta.Exceptions;
using NLog;

namespace Herta.Utils.WebsocketCreator
{
    public class WebsocketManager
    {
        private readonly WebSocket _webSocket;
        private readonly ArraySegment<byte> _buffer;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(WebsocketManager));
        private WebsocketState _state = WebsocketState.Idle; // 初始状态为 Idle
        private readonly string _id = Guid.NewGuid().ToString();

        public WebsocketManager(WebSocket webSocket)
        {
            _webSocket = webSocket;
            _buffer = new ArraySegment<byte>(new byte[8192]);
            _state = WebsocketState.Connected; // 连接建立后状态为 Connected
        }

        public async Task SendAsync(byte[] data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_state != WebsocketState.Connected && _state != WebsocketState.Idle)
            {
                throw new InvalidOperationException("Cannot send data when not connected or idle.");
            }
            _state = WebsocketState.Communicating; // 开始发送时状态切换为 Communicating
            _logger.Trace($"Sending data of length {data.Length}, Idle | Connected -> Communicating, id: {_id}");
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), messageType, endOfMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error sending data: {ex.Message}");
                _state = WebsocketState.Error;
                throw;
            }
            finally
            {
                _logger.Trace("Data sent. Communicating -> Idle");
                _state = WebsocketState.Idle; // 发送完成后状态切换回 Idle
            }
        }

        public async Task SendTextAsync(string message)
        {
            var encodedMessage = Encoding.UTF8.GetBytes(message);
            await SendAsync(encodedMessage, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SendJsonAsync<T>(T data)
        {
            var json = JsonSerializer.Serialize(data);
            await SendTextAsync(json);
        }

        public async Task SendBinAsync(byte[] data)
        {
            await SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public async Task CloseAsync(int code, string? reason = null)
        {
            if (_state != WebsocketState.Connected && _state != WebsocketState.Communicating && _state != WebsocketState.Idle)
            {
                throw new InvalidOperationException("Cannot close when not connected, idle, or communicating.");
            }
            _state = WebsocketState.Closing;
            _logger.Trace("Closing connection. Idle | Connected | Communicating -> Closing");
            try
            {
                await _webSocket.CloseAsync((WebSocketCloseStatus)code, reason, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error closing connection: {ex.Message}");
                _state = WebsocketState.Error;
                _logger.Trace("Closing connection. Closing -> Error");
                throw;
            }
            finally
            {
                _state = WebsocketState.Closed;
                _logger.Trace($"Connection closed. Closing -> Closed, id: {_id}");
            }
        }

        public async Task<string> ReceiveTextAsync()
        {
            if (_state != WebsocketState.Connected && _state != WebsocketState.Idle)
            {
                throw new InvalidOperationException("Cannot receive data when not connected or idle.");
            }
            _state = WebsocketState.Communicating; // 开始接收时状态切换为 Communicating
            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    try
                    {
                        result = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error receiving data: {ex.Message}");
                        _state = WebsocketState.Error;
                        _logger.Trace("Receiving data. Communicating -> Error");
                        throw;
                    }
                    var data = _buffer.Array ?? throw new WebsocketException("Buffer array is null");
                    ms.Write(data, _buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        _state = WebsocketState.Idle; // 接收完成后状态切换回 Idle
                        return await reader.ReadToEndAsync();
                    }
                }
                else
                {
                    throw new WebsocketException("Received non-text message");
                }
            }
        }

        public async Task<byte[]> ReceiveBinAsync()
        {
            if (_state != WebsocketState.Connected && _state != WebsocketState.Idle)
            {
                throw new InvalidOperationException("Cannot receive data when not connected or idle.");
            }
            _state = WebsocketState.Communicating; // 开始接收时状态切换为 Communicating
            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    try
                    {
                        result = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error receiving data: {ex.Message}");
                        _state = WebsocketState.Error;
                        _logger.Trace("Receiving data. Communicating -> Error");
                        throw;
                    }
                    var data = _buffer.Array ?? throw new WebsocketException("Buffer array is null");
                    ms.Write(data, _buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    _state = WebsocketState.Idle; // 接收完成后状态切换回 Idle
                    return ms.ToArray();
                }
                else
                {
                    throw new WebsocketException("Received non-binary message");
                }
            }
        }

        public WebsocketState State
        {
            get { return _state; }
        }
    }
}