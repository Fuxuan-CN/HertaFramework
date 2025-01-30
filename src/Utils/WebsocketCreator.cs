using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Herta.Models.Enums.WebsocketEnum;
using Herta.Exceptions;

namespace Herta.Utils.WebsocketCreator
{
    public class WebsocketManager
    {
        private readonly WebSocket _webSocket;
        private readonly ArraySegment<byte> _buffer;
        private WebsocketState _state = WebsocketState.Idle;

        public WebsocketManager(WebSocket webSocket)
        {
            _webSocket = webSocket;
            _buffer = new ArraySegment<byte>(new byte[8192]);
            _state = WebsocketState.Connected; // Assuming connection is established when object is created
        }

        public async Task SendAsync(byte[] data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_state != WebsocketState.Connected)
            {
                throw new InvalidOperationException("Cannot send data when not connected.");
            }
            _state = WebsocketState.Communicating;
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), messageType, endOfMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error and update state
                Console.WriteLine($"Error sending data: {ex.Message}");
                _state = WebsocketState.Error;
                throw;
            }
            finally
            {
                _state = WebsocketState.Idle;
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
            if (_state != WebsocketState.Connected && _state != WebsocketState.Communicating)
            {
                throw new InvalidOperationException("Cannot close when not connected or communicating.");
            }
            _state = WebsocketState.Closing;
            try
            {
                await _webSocket.CloseAsync((WebSocketCloseStatus)code, reason, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Log error and update state
                Console.WriteLine($"Error closing connection: {ex.Message}");
                _state = WebsocketState.Error;
                throw;
            }
            finally
            {
                _state = WebsocketState.Closed;
            }
        }

        public async Task<string> ReceiveTextAsync()
        {
            if (_state != WebsocketState.Connected)
            {
                throw new InvalidOperationException("Cannot receive data when not connected.");
            }
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
                        // Log error and update state
                        Console.WriteLine($"Error receiving data: {ex.Message}");
                        _state = WebsocketState.Error;
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
            if (_state != WebsocketState.Connected)
            {
                throw new InvalidOperationException("Cannot receive data when not connected.");
            }
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
                        // Log error and update state
                        Console.WriteLine($"Error receiving data: {ex.Message}");
                        _state = WebsocketState.Error;
                        throw;
                    }
                    var data = _buffer.Array ?? throw new WebsocketException("Buffer array is null");
                    ms.Write(data, _buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
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