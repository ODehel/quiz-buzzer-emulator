using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using QuizBuzzerEmulator.Models;
using QuizBuzzerEmulator.Services;

namespace QuizBuzzerEmulator.ViewModels;

public sealed class MainViewModel : BaseViewModel, IDisposable
{
    private readonly WebSocketService _wsService;
    private readonly EmulatorSettings _emulatorSettings;
    private readonly Dispatcher _dispatcher;
    private readonly Random _random = new();

    private string _connectionStatus = "Disconnected";
    private string _connectionColor = "#E74C3C";
    private string _gameState = "IDLE";
    private string _questionTitle = "";
    private string _questionType = "";
    private int _questionIndex;
    private int _timeRemaining;
    private int _score;
    private int _pointsEarned;
    private string _lastResult = "";
    private string _correctAnswer = "";
    private bool _isBuzzEnabled;
    private bool _isAnswerEnabled;
    private bool _isBuzzed;
    private bool _isEliminated;
    private bool _isAutoMode;
    private string _choiceA = "";
    private string _choiceB = "";
    private string _choiceC = "";
    private string _choiceD = "";

    public MainViewModel(
        WebSocketService wsService,
        EmulatorSettings emulatorSettings)
    {
        _wsService = wsService;
        _emulatorSettings = emulatorSettings;
        _dispatcher = Application.Current.Dispatcher;
        _isAutoMode = emulatorSettings.IsAutoMode;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => _wsService.State == ConnectionState.Disconnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => _wsService.State == ConnectionState.Connected);
        BuzzCommand = new AsyncRelayCommand(BuzzAsync, () => IsBuzzEnabled);
        AnswerCommand = new AsyncRelayCommand(
            async (param) => await AnswerAsync(param?.ToString() ?? ""),
            _ => IsAnswerEnabled);
        ToggleModeCommand = new RelayCommand(ToggleMode);

        _wsService.OnLog += msg => RunOnUI(() => AddLog(msg));
        _wsService.OnConnectionStateChanged += state => RunOnUI(() => UpdateConnectionState(state));
        _wsService.OnMessageReceived += (type, el) => RunOnUI(() => HandleMessage(type, el));
    }

    // --- Properties ---

    public ObservableCollection<string> Logs { get; } = new();

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public string ConnectionColor
    {
        get => _connectionColor;
        private set => SetProperty(ref _connectionColor, value);
    }

    public string GameState
    {
        get => _gameState;
        private set => SetProperty(ref _gameState, value);
    }

    public string QuestionTitle
    {
        get => _questionTitle;
        private set => SetProperty(ref _questionTitle, value);
    }

    public string QuestionType
    {
        get => _questionType;
        private set => SetProperty(ref _questionType, value);
    }

    public int QuestionIndex
    {
        get => _questionIndex;
        private set => SetProperty(ref _questionIndex, value);
    }

    public int TimeRemaining
    {
        get => _timeRemaining;
        private set => SetProperty(ref _timeRemaining, value);
    }

    public int Score
    {
        get => _score;
        private set => SetProperty(ref _score, value);
    }

    public int PointsEarned
    {
        get => _pointsEarned;
        private set => SetProperty(ref _pointsEarned, value);
    }

    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    public string CorrectAnswer
    {
        get => _correctAnswer;
        private set => SetProperty(ref _correctAnswer, value);
    }

    public bool IsBuzzEnabled
    {
        get => _isBuzzEnabled;
        private set
        {
            if (SetProperty(ref _isBuzzEnabled, value))
                ((AsyncRelayCommand)BuzzCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsAnswerEnabled
    {
        get => _isAnswerEnabled;
        private set
        {
            if (SetProperty(ref _isAnswerEnabled, value))
                ((AsyncRelayCommand)AnswerCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsBuzzed
    {
        get => _isBuzzed;
        private set => SetProperty(ref _isBuzzed, value);
    }

    public bool IsEliminated
    {
        get => _isEliminated;
        private set => SetProperty(ref _isEliminated, value);
    }

    public bool IsAutoMode
    {
        get => _isAutoMode;
        private set => SetProperty(ref _isAutoMode, value);
    }

    public string ChoiceA
    {
        get => _choiceA;
        private set => SetProperty(ref _choiceA, value);
    }

    public string ChoiceB
    {
        get => _choiceB;
        private set => SetProperty(ref _choiceB, value);
    }

    public string ChoiceC
    {
        get => _choiceC;
        private set => SetProperty(ref _choiceC, value);
    }

    public string ChoiceD
    {
        get => _choiceD;
        private set => SetProperty(ref _choiceD, value);
    }

    // --- Commands ---

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand BuzzCommand { get; }
    public ICommand AnswerCommand { get; }
    public ICommand ToggleModeCommand { get; }

    // --- Actions ---

    private async Task ConnectAsync()
    {
        ResetGameState();
        await _wsService.ConnectAsync();
    }

    private async Task DisconnectAsync() => await _wsService.DisconnectAsync();

    private async Task BuzzAsync()
    {
        if (!IsBuzzEnabled) return;
        await _wsService.SendBuzzAsync();
    }

    private async Task AnswerAsync(string value)
    {
        if (!IsAnswerEnabled || string.IsNullOrEmpty(value)) return;
        await _wsService.SendAnswerAsync(value);
        IsAnswerEnabled = false;
    }

    private void ToggleMode()
    {
        IsAutoMode = !IsAutoMode;
        AddLog($"Mode: {(IsAutoMode ? "AUTO" : "MANUAL")}");
    }

    // --- Message handling ---

    private void HandleMessage(string type, JsonElement element)
    {
        switch (type)
        {
            case "question_title":
                HandleQuestionTitle(element);
                break;
            case "question_open":
                HandleQuestionOpen(element);
                break;
            case "question_choices":
                HandleQuestionChoices(element);
                break;
            case "timer_tick":
                HandleTimerTick(element);
                break;
            case "timer_end":
                HandleTimerEnd();
                break;
            case "buzz_accepted":
                HandleBuzzAccepted();
                break;
            case "buzz_locked":
                HandleBuzzLocked(element);
                break;
            case "buzz_invalidated":
                HandleBuzzInvalidated();
                break;
            case "buzz_unlocked":
                HandleBuzzUnlocked(element);
                break;
            case "question_result":
                HandleQuestionResult(element);
                break;
            case "error":
                HandleError(element);
                break;
        }
    }

    private void HandleQuestionTitle(JsonElement el)
    {
        var msg = el.Deserialize<QuestionTitleMessage>()!;
        GameState = "QUESTION_TITLE";
        QuestionIndex = msg.QuestionIndex + 1;
        QuestionType = msg.QuestionType;
        QuestionTitle = msg.Title;
        TimeRemaining = msg.TimeLimit;
        LastResult = "";
        CorrectAnswer = "";
        PointsEarned = 0;
        IsBuzzed = false;
        IsEliminated = false;
        IsBuzzEnabled = false;
        IsAnswerEnabled = false;
        AddLog($"Question {QuestionIndex} [{QuestionType}]: {msg.Title}");
    }

    private void HandleQuestionOpen(JsonElement el)
    {
        var msg = el.Deserialize<QuestionOpenMessage>()!;
        GameState = "QUESTION_OPEN";
        QuestionIndex = msg.QuestionIndex + 1;
        QuestionType = msg.QuestionType;
        QuestionTitle = msg.Title;
        TimeRemaining = msg.TimeLimit;
        LastResult = "";
        CorrectAnswer = "";
        PointsEarned = 0;
        IsBuzzed = false;
        IsEliminated = false;

        if (msg.QuestionType == "SPEED")
        {
            IsBuzzEnabled = true;
            IsAnswerEnabled = false;
            AddLog($"Question {QuestionIndex} [SPEED]: {msg.Title} — BUZZ NOW!");

            if (IsAutoMode)
                _ = AutoBuzzAsync();
        }
    }

    private void HandleQuestionChoices(JsonElement el)
    {
        var msg = el.Deserialize<QuestionChoicesMessage>()!;
        GameState = "QUESTION_OPEN";
        TimeRemaining = msg.TimeLimit;

        ChoiceA = msg.Choices.Count > 0 ? msg.Choices[0] : "";
        ChoiceB = msg.Choices.Count > 1 ? msg.Choices[1] : "";
        ChoiceC = msg.Choices.Count > 2 ? msg.Choices[2] : "";
        ChoiceD = msg.Choices.Count > 3 ? msg.Choices[3] : "";

        IsBuzzEnabled = false;
        IsAnswerEnabled = true;
        AddLog($"Choices: A={ChoiceA}, B={ChoiceB}, C={ChoiceC}, D={ChoiceD}");

        if (IsAutoMode)
            _ = AutoAnswerAsync();
    }

    private void HandleTimerTick(JsonElement el)
    {
        var msg = el.Deserialize<TimerTickMessage>()!;
        TimeRemaining = msg.RemainingSeconds;
    }

    private void HandleTimerEnd()
    {
        GameState = "TIMER_END";
        TimeRemaining = 0;
        IsBuzzEnabled = false;
        IsAnswerEnabled = false;
        AddLog("Timer expired!");
    }

    private void HandleBuzzAccepted()
    {
        IsBuzzed = true;
        IsBuzzEnabled = false;
        GameState = "BUZZED";
        AddLog("BUZZ ACCEPTED! Waiting for validation...");
    }

    private void HandleBuzzLocked(JsonElement el)
    {
        var msg = el.Deserialize<BuzzLockedMessage>()!;
        IsBuzzEnabled = false;
        GameState = "BUZZ_LOCKED";
        AddLog($"Buzz locked by {msg.BuzzerUsername}");
    }

    private void HandleBuzzInvalidated()
    {
        IsEliminated = true;
        IsBuzzEnabled = false;
        GameState = "ELIMINATED";
        AddLog("Answer invalidated — eliminated for this question.");
    }

    private void HandleBuzzUnlocked(JsonElement el)
    {
        var msg = el.Deserialize<BuzzUnlockedMessage>()!;
        TimeRemaining = msg.RemainingSeconds;

        if (!IsEliminated)
        {
            IsBuzzEnabled = true;
            GameState = "QUESTION_OPEN";
            AddLog($"Buzz unlocked — {msg.RemainingSeconds}s remaining. BUZZ NOW!");

            if (IsAutoMode)
                _ = AutoBuzzAsync();
        }
    }

    private void HandleQuestionResult(JsonElement el)
    {
        var msg = el.Deserialize<QuestionResultMessage>()!;
        GameState = "RESULT";
        IsBuzzEnabled = false;
        IsAnswerEnabled = false;
        Score = msg.CumulativeScore;
        PointsEarned = msg.PointsEarned;
        CorrectAnswer = msg.CorrectAnswer;
        LastResult = msg.Correct ? "CORRECT" : "INCORRECT";

        AddLog(msg.Correct
            ? $"CORRECT! +{msg.PointsEarned} pts (total: {msg.CumulativeScore})"
            : $"INCORRECT. Answer was: {msg.CorrectAnswer} (total: {msg.CumulativeScore})");
    }

    private void HandleError(JsonElement el)
    {
        var msg = el.Deserialize<ErrorMessage>()!;
        AddLog($"ERROR [{msg.Code}]: {msg.Message}");
    }

    // --- Auto mode ---

    private async Task AutoBuzzAsync()
    {
        var delay = _random.Next(_emulatorSettings.AutoMinDelayMs, _emulatorSettings.AutoMaxDelayMs);
        AddLog($"[AUTO] Buzzing in {delay}ms...");
        await Task.Delay(delay);

        if (IsBuzzEnabled)
            await BuzzAsync();
    }

    private async Task AutoAnswerAsync()
    {
        var delay = _random.Next(_emulatorSettings.AutoMinDelayMs, _emulatorSettings.AutoMaxDelayMs);
        var choices = new[] { "A", "B", "C", "D" };
        var choice = choices[_random.Next(choices.Length)];
        AddLog($"[AUTO] Answering {choice} in {delay}ms...");
        await Task.Delay(delay);

        if (IsAnswerEnabled)
            await AnswerAsync(choice);
    }

    // --- Helpers ---

    private void UpdateConnectionState(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Connected:
                ConnectionStatus = "Connected";
                ConnectionColor = "#2ECC71";
                break;
            case ConnectionState.Connecting:
                ConnectionStatus = "Connecting...";
                ConnectionColor = "#F39C12";
                break;
            case ConnectionState.Disconnected:
                ConnectionStatus = "Disconnected";
                ConnectionColor = "#E74C3C";
                IsBuzzEnabled = false;
                IsAnswerEnabled = false;
                break;
        }

        ((AsyncRelayCommand)ConnectCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
    }

    private void ResetGameState()
    {
        GameState = "IDLE";
        QuestionTitle = "";
        QuestionType = "";
        QuestionIndex = 0;
        TimeRemaining = 0;
        LastResult = "";
        CorrectAnswer = "";
        PointsEarned = 0;
        IsBuzzed = false;
        IsEliminated = false;
        IsBuzzEnabled = false;
        IsAnswerEnabled = false;
        ChoiceA = "";
        ChoiceB = "";
        ChoiceC = "";
        ChoiceD = "";
    }

    private void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Add(entry);

        // Keep last 200 entries
        while (Logs.Count > 200)
            Logs.RemoveAt(0);
    }

    private void RunOnUI(Action action) => _dispatcher.BeginInvoke(action);

    public void Dispose() => _wsService.Dispose();
}
