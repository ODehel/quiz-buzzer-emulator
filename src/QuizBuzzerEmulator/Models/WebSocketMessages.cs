using System.Text.Json.Serialization;

namespace QuizBuzzerEmulator.Models;

// --- Base ---

public class WsMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

// --- Client → Server ---

public sealed class AuthMessage : WsMessage
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    public AuthMessage() => Type = "auth";
}

public sealed class BuzzMessage : WsMessage
{
    public BuzzMessage() => Type = "buzz";
}

public sealed class AnswerMessage : WsMessage
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    public AnswerMessage() => Type = "answer";
}

// --- Server → Client ---

public sealed class AuthSuccessMessage : WsMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public sealed class QuestionTitleMessage : WsMessage
{
    [JsonPropertyName("question_index")]
    public int QuestionIndex { get; set; }

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; }
}

public sealed class QuestionOpenMessage : WsMessage
{
    [JsonPropertyName("question_index")]
    public int QuestionIndex { get; set; }

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("started_at")]
    public string StartedAt { get; set; } = "";

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; }
}

public sealed class QuestionChoicesMessage : WsMessage
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; set; } = [];

    [JsonPropertyName("started_at")]
    public string StartedAt { get; set; } = "";

    [JsonPropertyName("time_limit")]
    public int TimeLimit { get; set; }
}

public sealed class TimerTickMessage : WsMessage
{
    [JsonPropertyName("remaining_seconds")]
    public int RemainingSeconds { get; set; }
}

public sealed class BuzzAcceptedMessage : WsMessage;

public sealed class BuzzLockedMessage : WsMessage
{
    [JsonPropertyName("buzzer_username")]
    public string BuzzerUsername { get; set; } = "";
}

public sealed class BuzzInvalidatedMessage : WsMessage;

public sealed class BuzzUnlockedMessage : WsMessage
{
    [JsonPropertyName("remaining_seconds")]
    public int RemainingSeconds { get; set; }
}

public sealed class QuestionResultMessage : WsMessage
{
    [JsonPropertyName("correct_answer")]
    public string CorrectAnswer { get; set; } = "";

    [JsonPropertyName("player_answer")]
    public string? PlayerAnswer { get; set; }

    [JsonPropertyName("correct")]
    public bool Correct { get; set; }

    [JsonPropertyName("points_earned")]
    public int PointsEarned { get; set; }

    [JsonPropertyName("cumulative_score")]
    public int CumulativeScore { get; set; }
}

public sealed class GameResumedMessage : WsMessage
{
    [JsonPropertyName("question_index")]
    public int QuestionIndex { get; set; }

    [JsonPropertyName("cumulative_score")]
    public int CumulativeScore { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}

public sealed class TimerEndMessage : WsMessage;

public sealed class ErrorMessage : WsMessage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

// --- Auth HTTP ---

public sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";
}

public sealed class TokenRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}
