using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bookkeeping.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Cache view models to preserve state
    private readonly DashboardViewModel _dashboardViewModel = new();
    private readonly TransactionsViewModel _transactionsViewModel = new();
    private readonly ImportViewModel _importViewModel = new();
    private readonly AnalyticsViewModel _analyticsViewModel = new();
    private readonly CategoriesViewModel _categoriesViewModel = new();
    private readonly StatisticsViewModel _statisticsViewModel = new();
    private readonly AccountsViewModel _accountsViewModel = new();
    private readonly SettingsViewModel _settingsViewModel = new();

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private string _selectedPage = "Dashboard";

    public MainWindowViewModel()
    {
        _currentPage = _dashboardViewModel;
    }

    [RelayCommand]
    private void NavigateTo(string pageName)
    {
        SelectedPage = pageName;
        CurrentPage = pageName switch
        {
            "Dashboard" => _dashboardViewModel,
            "Transactions" => _transactionsViewModel,
            "Import" => _importViewModel,
            "Analytics" => _analyticsViewModel,
            "Categories" => _categoriesViewModel,
            "统计" => _statisticsViewModel,
            "Accounts" => _accountsViewModel,
            "Settings" => _settingsViewModel,
            _ => CurrentPage
        };
    }
}