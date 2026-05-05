using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMap.App.ViewModels.Pages;
using MoneyMap.Import;
using MoneyMap.Import.Detection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MoneyMap.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Cache view models to preserve state
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly TransactionsViewModel _transactionsViewModel;
    private readonly ImportViewModel _importViewModel;
    private readonly AnalyticsViewModel _analyticsViewModel;
    private readonly CategoriesViewModel _categoriesViewModel;
    private readonly StatisticsViewModel _statisticsViewModel;
    private readonly AccountsViewModel _accountsViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private ViewModelBase _currentPage = null!;

    [ObservableProperty]
    private string _selectedPage = "Dashboard";

    public MainWindowViewModel(IServiceScopeFactory scopeFactory)
    {
        // 从 scopeFactory 创建一个 scope 来获取 Singleton 服务
        using var scope = scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        _dashboardViewModel = new DashboardViewModel();
        _transactionsViewModel = new TransactionsViewModel(scopeFactory);
        _importViewModel = new ImportViewModel(serviceProvider.GetRequiredService<SourceDetector>(), serviceProvider.GetRequiredService<ParserFactory>(), scopeFactory);
        _analyticsViewModel = new AnalyticsViewModel();
        _categoriesViewModel = new CategoriesViewModel(scopeFactory);
        _statisticsViewModel = new StatisticsViewModel(scopeFactory);
        _accountsViewModel = new AccountsViewModel(scopeFactory);
        _settingsViewModel = new SettingsViewModel(scopeFactory);

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

        if (CurrentPage is ImportViewModel importVm)
        {
            importVm.OnBecameCurrent();
        }
    }
}
