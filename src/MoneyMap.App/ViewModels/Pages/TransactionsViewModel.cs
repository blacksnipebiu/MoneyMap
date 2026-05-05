using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using MoneyMap.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyMap.App.ViewModels.Pages;

public partial class TransactionsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private string _searchKeyword = "";

    [ObservableProperty]
    private DateTime? _dateFrom;

    [ObservableProperty]
    private DateTime? _dateTo;

    [ObservableProperty]
    private TransactionType? _selectedType;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    private int _totalPages;
    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(HasNextPage));
            }
        }
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasData;

    public bool HasNextPage => CurrentPage < TotalPages;

    public ObservableCollection<Transaction> Transactions { get; } = new();

    public TransactionType[] TypeOptions { get; } = new[] { TransactionType.Expense, TransactionType.Income, TransactionType.Transfer };

public TransactionsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadDataAsync();
        }
    }

[RelayCommand]
    private async Task DeleteAsync(Transaction transaction)
    {
        if (transaction == null) return;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        await repo.DeleteAsync(transaction.Id);
        
        Transactions.Remove(transaction);
        TotalCount--;
        App.ToastService.ShowSuccess("已删除交易记录");
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchKeyword = "";
        DateFrom = null;
        DateTo = null;
        SelectedType = null;
        _ = RefreshAsync();
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

            var filter = new TransactionFilter
            {
                SearchKeyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword,
                DateFrom = DateFrom,
                DateTo = DateTo,
                Type = SelectedType
            };

            var result = await repo.GetFilteredAsync(filter, CurrentPage, PageSize);

            Transactions.Clear();
            foreach (var t in result.Items)
            {
                Transactions.Add(t);
            }

            TotalCount = result.TotalCount;
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize);
            HasData = result.TotalCount > 0;
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("加载失败：" + ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
