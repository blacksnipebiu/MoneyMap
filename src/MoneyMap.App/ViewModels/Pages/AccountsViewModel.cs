using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyMap.App.ViewModels.Pages;

public partial class AccountsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _newAccountName = "";

    [ObservableProperty]
    private DataSource _newAccountSource = DataSource.Alipay;

    [ObservableProperty]
    private string _newAccountLastFourDigits = "";

    [ObservableProperty]
    private bool _isAddingNew;

    [ObservableProperty]
    private Account? _editingAccount;

    [ObservableProperty]
    private string _editAccountName = "";

    [ObservableProperty]
    private DataSource _editAccountSource = DataSource.Alipay;

    [ObservableProperty]
    private string _editAccountLastFourDigits = "";

    public ObservableCollection<Account> Accounts { get; } = new();

    public DataSource[] SourceOptions { get; } = Enum.GetValues<DataSource>();

    public string SourceDisplayName => GetSourceDisplayName(NewAccountSource);

private readonly IServiceScopeFactory _scopeFactory;

    public AccountsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var accounts = await repo.GetAllAsync();

            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("加载账户失败：" + ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        IsAddingNew = true;
        NewAccountName = "";
        NewAccountSource = DataSource.Alipay;
        NewAccountLastFourDigits = "";
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddingNew = false;
        NewAccountName = "";
        NewAccountSource = DataSource.Alipay;
        NewAccountLastFourDigits = "";
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            App.ToastService.ShowError("请输入账户名称");
            return;
        }

        try
        {
            var account = new Account
            {
                Name = NewAccountName.Trim(),
                Source = NewAccountSource,
                LastFourDigits = string.IsNullOrWhiteSpace(NewAccountLastFourDigits) ? null : NewAccountLastFourDigits.Trim(),
                CreatedAt = DateTime.Now
            };

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            await repo.AddAsync(account);

            Accounts.Add(account);
            CancelAdd();
            App.ToastService.ShowSuccess("账户添加成功");
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("添加失败：" + ex.Message);
        }
    }

    [RelayCommand]
    private void StartEdit(Account account)
    {
        if (account == null) return;
        EditingAccount = account;
        EditAccountName = account.Name;
        EditAccountSource = account.Source;
        EditAccountLastFourDigits = account.LastFourDigits ?? "";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingAccount = null;
        EditAccountName = "";
        EditAccountSource = DataSource.Alipay;
        EditAccountLastFourDigits = "";
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (EditingAccount == null) return;

        if (string.IsNullOrWhiteSpace(EditAccountName))
        {
            App.ToastService.ShowError("请输入账户名称");
            return;
        }

        try
        {
            EditingAccount.Name = EditAccountName.Trim();
            EditingAccount.Source = EditAccountSource;
            EditingAccount.LastFourDigits = string.IsNullOrWhiteSpace(EditAccountLastFourDigits) ? null : EditAccountLastFourDigits.Trim();

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            await repo.UpdateAsync(EditingAccount);

            // Refresh the list
            var existingAccount = Accounts.FirstOrDefault(a => a.Id == EditingAccount.Id);
            if (existingAccount != null)
            {
                var index = Accounts.IndexOf(existingAccount);
                Accounts[index] = EditingAccount;
            }

            CancelEdit();
            App.ToastService.ShowSuccess("账户更新成功");
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("更新失败：" + ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Account account)
    {
        if (account == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            await repo.DeleteAsync(account.Id);

            Accounts.Remove(account);
            App.ToastService.ShowSuccess("账户已删除");
        }
        catch (Exception ex)
        {
            App.ToastService.ShowError("删除失败：" + ex.Message);
        }
    }

    public static string GetSourceDisplayName(DataSource source)
    {
        return source switch
        {
            DataSource.Alipay => "支付宝",
            DataSource.WeChatPay => "微信支付",
            DataSource.BankCard => "银行卡",
            DataSource.Manual => "手动录入",
            DataSource.Other => "其他",
            _ => source.ToString()
        };
    }

    public static string GetSourceIcon(DataSource source)
    {
        return source switch
        {
            DataSource.Alipay => "💙",
            DataSource.WeChatPay => "💚",
            DataSource.BankCard => "💳",
            DataSource.Manual => "✏️",
            DataSource.Other => "📁",
            _ => "📄"
        };
    }
}

