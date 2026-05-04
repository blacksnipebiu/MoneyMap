using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Bookkeeping.App.ViewModels;
using Bookkeeping.App.Views;
using Bookkeeping.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Bookkeeping.Data;
using Bookkeeping.Import;
using Bookkeeping.Core.Interfaces;

namespace Bookkeeping.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static ToastService ToastService { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Setup DI
            var services = new ServiceCollection();
            
            // Data layer
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Bookkeeping", "bookkeeping.db");
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            services.AddBookkeepingData(dbPath);
            services.AddBookkeepingImport();
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddSingleton<ToastService>();
            
            Services = services.BuildServiceProvider();
            ToastService = Services.GetRequiredService<ToastService>();
            
            // Initialize database
            Services.InitializeDatabaseAsync().Wait();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}