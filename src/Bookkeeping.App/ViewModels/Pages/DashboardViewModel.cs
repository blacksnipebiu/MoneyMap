using CommunityToolkit.Mvvm.ComponentModel;

namespace Bookkeeping.App.ViewModels.Pages;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private decimal _monthIncome = 11137.15m;

    [ObservableProperty]
    private decimal _monthExpense = 130691.10m;

    [ObservableProperty]
    private int _transactionCount = 2698;

    public decimal MonthBalance => MonthIncome - MonthExpense;
}
