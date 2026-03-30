namespace QuizBuzzerEmulator.Models;

public sealed class HubSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3000;
    public bool UseSsl { get; set; }

    public string BaseHttpUrl => $"{(UseSsl ? "https" : "http")}://{Host}:{Port}";
    public string WebSocketUrl => $"{(UseSsl ? "wss" : "ws")}://{Host}:{Port}/ws";
}

public sealed class BuzzerSettings
{
    public string Username { get; set; } = "quiz_buzzer_01";
    public string Password { get; set; } = "";
}

public sealed class EmulatorSettings
{
    public string Mode { get; set; } = "Manual";
    public int AutoMinDelayMs { get; set; } = 500;
    public int AutoMaxDelayMs { get; set; } = 3000;
    public int ReconnectAttempts { get; set; } = 3;
    public int ReconnectDelayMs { get; set; } = 2000;

    public bool IsAutoMode => Mode.Equals("Auto", StringComparison.OrdinalIgnoreCase);
}
