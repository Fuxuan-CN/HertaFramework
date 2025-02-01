using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Herta.Models.Enums.WebsocketEnum;
using Herta.Utils.Logger;
using Herta.Exceptions.WebsocketExceprions.WebsocketClosedException;
using NLog;

namespace Herta.Utils.WebsocketCreator
{
    public class WebsocketManager : IDisposable
    {
        private readonly WebSocket _webSocket;
        private readonly ArraySegment<byte> _buffer;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(WebsocketManager));
        private WebsocketState _state = WebsocketState.Closed; // 初始状态为 Closed
        private readonly Guid _id = Guid.NewGuid();
        private readonly object _stateLock = new object();
        public Dictionary<string, string?> parameters { get; set; }

        public WebsocketManager(WebSocket webSocket, Dictionary<string, string?> parameters)
        {
            _webSocket = webSocket;
            _buffer = new ArraySegment<byte>(new byte[8192]);
            _state = WebsocketState.Connected; // 连接建立后状态为 Connected
            _logger.Trace($"Websocket created. Id: {_id}");
            this.parameters = parameters;
        }

        public async Task SendAsync(byte[] data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await ExecuteWithStateCheck(async () =>
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), messageType, endOfMessage, cancellationToken);
            }, "Sending data");
        }

        public async Task SendTextAsync(string message)
        {
            await SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SendJsonAsync<T>(T data)
        {
            await SendTextAsync(JsonSerializer.Serialize(data));
        }

        public async Task SendBinAsync(byte[] data)
        {
            await SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public async Task CloseAsync(int code, string? reason = null)
        {
            await ExecuteWithStateCheck(async () =>
            {
                await _webSocket.CloseAsync((WebSocketCloseStatus)code, reason, CancellationToken.None);
            }, "Closing connection");
        }

        public async Task<string> ReceiveTextAsync()
        {
            return await ExecuteWithStateCheck(async () =>
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new WebsocketClosedException("WebSocket connection closed.");
                        }
                        ms.Write(_buffer.Array!, _buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }, "Receiving text data");
        }

        public async Task<byte[]> ReceiveBinAsync()
        {
            return await ExecuteWithStateCheck(async () =>
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new WebsocketClosedException("WebSocket connection closed.");
                        }
                        ms.Write(_buffer.Array!, _buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    return ms.ToArray();
                }
            }, "Receiving binary data");
        }

        public WebsocketState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        private async Task<T> ExecuteWithStateCheck<T>(Func<Task<T>> action, string actionName)
        {
            lock (_stateLock)
            {
                if (_state != WebsocketState.Connected && _state != WebsocketState.Idle)
                {
                    throw new InvalidOperationException($"Cannot {actionName.ToLower()} when not connected or idle.");
                }
                _state = WebsocketState.Communicating;
                _logger.Trace($"{actionName}. id: {_id}");
            }

            try
            {
                return await action();
            }
            catch (WebSocketException wsEx)
            {
                _logger.Warn($"{actionName} failed: {wsEx.Message}");
                lock (_stateLock)
                {
                    _state = WebsocketState.Closed;
                }
                throw new WebsocketClosedException($"{actionName} failed: WebSocket connection closed unexpectedly", wsEx);
            }
            catch (Exception ex)
            {
                _logger.Warn($"{actionName} failed: {ex.Message}");
                lock (_stateLock)
                {
                    _state = WebsocketState.Error;
                }
                throw;
            }
            finally
            {
                lock (_stateLock)
                {
                    _state = WebsocketState.Idle;
                    _logger.Trace($"{actionName} completed.");
                }
            }
        }

        private async Task ExecuteWithStateCheck(Func<Task> action, string actionName)
        {
            await ExecuteWithStateCheck(async () =>
            {
                await action();
                return true; // 返回一个默认值
            }, actionName);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }
}
