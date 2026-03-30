using System.Net.Http;
using System.Text;
using System.Text.Json;
using QuizBuzzerEmulator.Models;

namespace QuizBuzzerEmulator.Services;

public sealed class AuthenticationService
{
    private readonly HubSettings _hubSettings;
    private readonly BuzzerSettings _buzzerSettings;
    private readonly HttpClient _httpClient;

    private string? _token;
    private DateTime _tokenExpiry;

    public AuthenticationService(HubSettings hubSettings, BuzzerSettings buzzerSettings)
    {
        _hubSettings = hubSettings;
        _buzzerSettings = buzzerSettings;
        _httpClient = new HttpClient { BaseAddress = new Uri(hubSettings.BaseHttpUrl) };
    }

    public string? Token => _token;
    public bool IsTokenValid => _token != null && DateTime.UtcNow < _tokenExpiry;

    public async Task<string> GetTokenAsync()
    {
        if (IsTokenValid)
            return _token!;

        var request = new TokenRequest
        {
            Username = _buzzerSettings.Username,
            Password = _buzzerSettings.Password
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new AuthenticationException(
                $"Authentication failed ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseJson)
            ?? throw new AuthenticationException("Invalid token response from server.");

        _token = tokenResponse.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

        return _token;
    }

    public void InvalidateToken()
    {
        _token = null;
        _tokenExpiry = DateTime.MinValue;
    }
}

public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
