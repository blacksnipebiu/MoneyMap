using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bookkeeping.App.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public partial class ToastItem : ObservableObject
{
    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private ToastType _type;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private double _opacity = 1.0;

    public Guid Id { get; } = Guid.NewGuid();
    public int Duration { get; set; } = 3000;

    public ToastItem(string message, ToastType type, int duration = 3000)
    {
        Message = message;
        Type = type;
        Duration = duration;
    }
}

public class ToastService
{
    private readonly ObservableCollection<ToastItem> _toasts = new();
    public ObservableCollection<ToastItem> Toasts => _toasts;

    public void Show(string message, ToastType type = ToastType.Info, int duration = 3000)
    {
        var toast = new ToastItem(message, type, duration);
        _toasts.Add(toast);
        
        // Auto-remove after duration
        _ = AutoRemoveAsync(toast);
    }

    public void ShowSuccess(string message, int duration = 3000)
    {
        Show(message, ToastType.Success, duration);
    }

    public void ShowError(string message, int duration = 4000)
    {
        Show(message, ToastType.Error, duration);
    }

    public void ShowWarning(string message, int duration = 3500)
    {
        Show(message, ToastType.Warning, duration);
    }

    public void ShowInfo(string message, int duration = 3000)
    {
        Show(message, ToastType.Info, duration);
    }

    private async Task AutoRemoveAsync(ToastItem toast)
    {
        await Task.Delay(toast.Duration);
        
        // Fade out animation
        for (int i = 10; i >= 0; i--)
        {
            toast.Opacity = i / 10.0;
            await Task.Delay(30);
        }
        
        toast.IsVisible = false;
        _toasts.Remove(toast);
    }

    public void Remove(ToastItem toast)
    {
        _toasts.Remove(toast);
    }
}