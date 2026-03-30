using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using QuizBuzzerEmulator.Models;

namespace QuizBuzzerEmulator.Services;

public sealed class WebSocketService : IDisposable
{
    private readonly HubSettings _hubSettings;
    private readonly EmulatorSettings _emulatorSettings;
    private readonly AuthenticationService _authService;

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    public event Action<string>? OnLog;
    public event Action<ConnectionState>? OnConnectionStateChanged;
    public event Action<string, JsonElement>? OnMessageReceived;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public WebSocketService(
        HubSettings hubSettings,
        EmulatorSettings emulatorSettings,
        AuthenticationService authService)
    {
        _hubSettings = hubSettings;
        _emulatorSettings = emulatorSettings;
        _authService = authService;
    }

    public async Task ConnectAsync()
    {
        _cts = new CancellationTokenSource();

        for (var attempt = 1; attempt <= _emulatorSettings.ReconnectAttempts + 1; attempt++)
        {
            try
            {
                SetState(ConnectionState.Connecting);
                Log($"Connection attempt {attempt}...");

                // Step 1: Get JWT token via HTTP
                var token = await _authService.GetTokenAsync();
                Log("JWT token obtained.");

                // Step 2: Connect WebSocket
                _ws = new ClientWebSocket();
                var uri = new Uri(_hubSettings.WebSocketUrl);
                await _ws.ConnectAsync(uri, _cts.Token);
                Log($"WebSocket connected to {uri}");

                // Step 3: Send auth message
                var authMsg = new AuthMessage { Token = token };
                await SendMessageAsync(authMsg);
                Log("Auth message sent, waiting for confirmation...");

                // Step 4: Wait for auth_success
                var (type, element) = await ReceiveOneMessageAsync(_cts.Token);

                if (type == "auth_success")
                {
                    var authSuccess = element.Deserialize<AuthSuccessMessage>();
                    Log($"Authenticated as {authSuccess?.Username} (role: {authSuccess?.Role}, expires in: {authSuccess?.ExpiresIn}s)");
                    SetState(ConnectionState.Connected);

                    // Start receiving messages
                    _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                    return;
                }
                else
                {
                    Log($"Unexpected response: {type}");
                    await DisconnectInternalAsync();
                }
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
                await DisconnectInternalAsync();

                if (attempt <= _emulatorSettings.ReconnectAttempts)
                {
                    var delay = _emulatorSettings.ReconnectDelayMs * attempt;
                    Log($"Retrying in {delay}ms...");
                    await Task.Delay(delay);
                }
            }
        }

        SetState(ConnectionState.Disconnected);
        Log("All connection attempts exhausted.");
    }

    public async Task DisconnectAsync()
    {
        Log("Disconnecting...");
        _cts?.Cancel();

        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch { /* expected */ }
        }

        await DisconnectInternalAsync();
        SetState(ConnectionState.Disconnected);
        Log("Disconnected.");
    }

    public async Task SendBuzzAsync()
    {
        await SendMessageAsync(new BuzzMessage());
        Log(">>> BUZZ sent");
    }

    public async Task SendAnswerAsync(string value)
    {
        await SendMessageAsync(new AnswerMessage { Value = value });
        Log($">>> Answer sent: {value}");
    }

    private async Task SendMessageAsync(object message)
    {
        if (_ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected.");

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
    }

    private async Task<(string type, JsonElement element)> ReceiveOneMessageAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await _ws!.ReceiveAsync(buffer, ct);
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            var reason = _ws.CloseStatusDescription ?? "Unknown";
            throw new WebSocketException($"Server closed connection: {_ws.CloseStatus} - {reason}");
        }

        var raw = sb.ToString();
        var doc = JsonDocument.Parse(raw);
        var type = doc.RootElement.GetProperty("type").GetString() ?? "";
        return (type, doc.RootElement.Clone());
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                var (type, element) = await ReceiveOneMessageAsync(ct);
                Log($"<<< {type}");
                OnMessageReceived?.Invoke(type, element);
            }
        }
        catch (OperationCanceledException) { /* normal disconnect */ }
        catch (WebSocketException ex)
        {
            Log($"WebSocket error: {ex.Message}");
            HandleDisconnection();
        }
        catch (Exception ex)
        {
            Log($"Receive error: {ex.Message}");
            HandleDisconnection();
        }
    }

    private void HandleDisconnection()
    {
        if (State == ConnectionState.Disconnected) return;

        SetState(ConnectionState.Disconnected);
        Log("Connection lost. Use Connect to reconnect.");
    }

    private async Task DisconnectInternalAsync()
    {
        if (_ws != null)
        {
            try
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
            }
            catch { /* best effort */ }

            _ws.Dispose();
            _ws = null;
        }
    }

    private void SetState(ConnectionState state)
    {
        State = state;
        OnConnectionStateChanged?.Invoke(state);
    }

    private void Log(string message) => OnLog?.Invoke(message);

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _ws?.Dispose();
    }
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected
}
