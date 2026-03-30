using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using QuizBuzzerEmulator.Models;
using QuizBuzzerEmulator.Services;
using QuizBuzzerEmulator.ViewModels;
using QuizBuzzerEmulator.Views;

namespace QuizBuzzerEmulator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var hubSettings = new HubSettings();
        config.GetSection("Hub").Bind(hubSettings);

        var buzzerSettings = new BuzzerSettings();
        config.GetSection("Buzzer").Bind(buzzerSettings);

        var emulatorSettings = new EmulatorSettings();
        config.GetSection("Emulator").Bind(emulatorSettings);

        // Build services
        var authService = new AuthenticationService(hubSettings, buzzerSettings);
        var wsService = new WebSocketService(hubSettings, emulatorSettings, authService);

        // Build ViewModel
        var viewModel = new MainViewModel(wsService, emulatorSettings);

        // Show window
        var mainWindow = new MainWindow(viewModel);
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
