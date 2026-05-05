using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoneyMap.App.ViewModels.Pages;

public partial class AnalyticsViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedPeriod = "本月";
}
