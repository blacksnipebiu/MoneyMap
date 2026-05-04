using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Bookkeeping.App.Services;

namespace Bookkeeping.App.Controls;

public partial class ToastControl : UserControl
{
    public ToastControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ToastItem toast)
        {
            // Set background color based on type
            ToastBorder.Background = toast.Type switch
            {
                ToastType.Success => new SolidColorBrush(Color.Parse("#10B981")),
                ToastType.Error => new SolidColorBrush(Color.Parse("#EF4444")),
                ToastType.Warning => new SolidColorBrush(Color.Parse("#F59E0B")),
                ToastType.Info => new SolidColorBrush(Color.Parse("#3B82F6")),
                _ => new SolidColorBrush(Color.Parse("#323232"))
            };

            // Set icon based on type
            IconText.Text = toast.Type switch
            {
                ToastType.Success => "✓",
                ToastType.Error => "✕",
                ToastType.Warning => "⚠",
                ToastType.Info => "ℹ",
                _ => ""
            };
        }
    }
}