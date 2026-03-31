using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using QuizBuzzerEmulator.ViewModels;

namespace QuizBuzzerEmulator.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll logs to bottom
        viewModel.Logs.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[^1]);
            }
        };

        Closed += (_, _) => viewModel.Dispose();
    }
}
