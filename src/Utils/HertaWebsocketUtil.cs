using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Herta.Models.Enums.WebsocketEnum;
using Herta.Utils.Logger;
using Herta.Exceptions.WebsocketExceptions;
using NLog;

namespace Herta.Utils.HertaWebsocketUtil;

public sealed class HertaWebsocket : IDisposable
{
    private readonly WebSocket _webSocket;
    private ArraySegment<byte> _buffer;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HertaWebsocket));
    private WebsocketState _state = WebsocketState.Closed;
    private readonly Guid _id = Guid.NewGuid();
    private readonly object _stateLock = new object();
    public Dictionary<string, string?> Parameters { get; set; }
    public Guid Id => _id;
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
    // Events
    public event Func<string, Task>? OnTextReceivedAsync;
    public event Func<byte[], Task>? OnBinaryReceivedAsync;
    public event EventHandler<Exception>? OnError;
    public event EventHandler<WebSocketCloseStatus>? OnClosed;
    public event EventHandler? OnConnected;

    public HertaWebsocket(WebSocket webSocket, Dictionary<string, string?> parameters)
    {
        _webSocket = webSocket;
        _buffer = new ArraySegment<byte>(new byte[8192]); // Default buffer size
        _state = WebsocketState.Connected;
        _logger.Trace($"Websocket created. Id: {_id}");
        Parameters = parameters;
        OnConnected?.Invoke(this, EventArgs.Empty);
    }

    public bool IsConnected()
    {
        return ReadStateWithLock(state => state == WebsocketState.Connected || state == WebsocketState.Communicating || state == WebsocketState.Idle);
    }

    public bool IsError()
    {
        return ReadStateWithLock(state => state == WebsocketState.Error);
    }

    public bool IsClosing()
    {
        return ReadStateWithLock(state => state == WebsocketState.Closing);
    }

    public bool IsClosed()
    {
        return ReadStateWithLock(state => state == WebsocketState.Closed);
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
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));
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
        }, $"Closing connection with reason: {reason}");
        OnClosed?.Invoke(this, (WebSocketCloseStatus)code);
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
                    var message = await reader.ReadToEndAsync();
                    await (OnTextReceivedAsync ?? (_ => Task.CompletedTask)).Invoke(message);
                    return message;
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

                var data = ms.ToArray();
                await (OnBinaryReceivedAsync ?? (_ => Task.CompletedTask)).Invoke(data);
                return data;
            }
        }, "Receiving binary data");
    }

    private T ReadStateWithLock<T>(Func<WebsocketState, T> stateCheckFunc)
    {
        lock (_stateLock)
        {
            return stateCheckFunc(_state);
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
            OnError?.Invoke(this, wsEx);
            throw new WebsocketClosedException($"{actionName} failed: WebSocket connection closed unexpectedly", wsEx);
        }
        catch (Exception ex)
        {
            _logger.Warn($"{actionName} failed: {ex.Message}");
            lock (_stateLock)
            {
                _state = WebsocketState.Error;
            }
            OnError?.Invoke(this, ex);
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
