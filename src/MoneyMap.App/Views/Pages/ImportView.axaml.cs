using MoneyMap.App.ViewModels.Pages;
using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using MoneyMap.App.ViewModels;
using MoneyMap.Core.Models;

namespace MoneyMap.App.Views.Pages;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        DragDrop.AddDragOverHandler(DropZone, OnDragOver);
        DragDrop.AddDropHandler(DropZone, OnDrop);
        DragDrop.AddDragEnterHandler(DropZone, OnDragEnter);
        DragDrop.AddDragLeaveHandler(DropZone, OnDragLeave);
    }

    private void OnHistoryFileDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ImportRecord record)
        {
            if (DataContext is ImportViewModel vm)
            {
                vm.OpenHistoryFileCommand.Execute(record);
            }
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is ImportViewModel vm)
        {
            vm.IsDragOver = true;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is ImportViewModel vm)
        {
            vm.IsDragOver = false;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null)
            {
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                    if (extension == ".csv" || extension == ".xlsx" || extension == ".xls")
                    {
                        e.DragEffects = DragDropEffects.Copy;
                        return;
                    }
                }
            }
        }
        e.DragEffects = DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ImportViewModel vm) return;

        vm.IsDragOver = false;

        if (e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null)
            {
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                    if (extension == ".csv" || extension == ".xlsx" || extension == ".xls")
                    {
                        vm.SetFile(file.Path.LocalPath);
                    }
                }
            }
        }
    }

    private void OnFilterFlyoutCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ImportViewModel vm)
        {
            vm.ActiveFilterColumn = null;
        }
    }

    private void OnFilterFlyoutClosed(object? sender, EventArgs e)
    {
        if (DataContext is ImportViewModel vm)
        {
            vm.ActiveFilterColumn = null;
        }
    }
}

