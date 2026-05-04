using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Bookkeeping.App.ViewModels;

namespace Bookkeeping.App.Views;

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
}