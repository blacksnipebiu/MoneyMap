using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Bookkeeping.App.Models;

namespace Bookkeeping.App.Views.Dialogs;

public partial class EmojiPickerDialog : Window
{
    private string? _selectedEmoji;

    public EmojiPickerDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public static async Task<string?> ShowDialogAsync(Window parent, string? currentEmoji)
    {
        var dialog = new EmojiPickerDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        // Set initial selection
        if (!string.IsNullOrEmpty(currentEmoji))
        {
            dialog.SetSelectedEmoji(currentEmoji);
        }

        dialog.Owner = parent;

        return await dialog.ShowDialog<string?>(parent);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set up category tabs
        foreach (var category in EmojiData.Categories)
        {
            CategoryTabs.Items.Add(category);
        }

        // Select first category
        if (CategoryTabs.Items.Count > 0)
        {
            CategoryTabs.SelectedIndex = 0;
        }

        CategoryTabs.SelectionChanged += OnCategoryChanged;

        // Load emojis for first category
        LoadEmojisForCategory(EmojiData.Categories.FirstOrDefault() ?? "");
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryTabs.SelectedItem is string category)
        {
            LoadEmojisForCategory(category);
        }
    }

    private void LoadEmojisForCategory(string category)
    {
        var emojis = EmojiData.GetByCategory(category);
        EmojiItems.ItemsSource = emojis;
    }

    private void SetSelectedEmoji(string emoji)
    {
        _selectedEmoji = emoji;

        // Find and highlight the selected emoji button
        if (EmojiItems.ItemsSource is IReadOnlyList<EmojiItem> emojis)
        {
            for (int i = 0; i < emojis.Count; i++)
            {
                if (emojis[i].Emoji == emoji)
                {
                    // Select the appropriate tab
                    var item = emojis[i];
                    CategoryTabs.SelectedItem = item.Category;
                    break;
                }
            }
        }
    }

    private void OnEmojiClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string emoji)
        {
            _selectedEmoji = emoji;
            Close(_selectedEmoji);
        }
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

