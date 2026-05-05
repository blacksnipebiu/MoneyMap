using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MoneyMap.App.ViewModels;
using MoneyMap.App.Views;
using MoneyMap.App.Services;
using Microsoft.Extensions.DependencyInjection;
using MoneyMap.Data;
using MoneyMap.Data.Services;
using MoneyMap.Import;
using MoneyMap.Core.Interfaces;
using MoneyMap.Core.Services;

namespace MoneyMap.App;

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
                "MoneyMap", "MoneyMap.db");
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            services.AddMoneyMapData(dbPath);
            services.AddMoneyMapImport();
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddSingleton<ToastService>();

            // Business services
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IStatisticsService, StatisticsService>();

            Services = services.BuildServiceProvider();
            ToastService = Services.GetRequiredService<ToastService>();
            
            // Initialize database
            Services.InitializeDatabaseAsync().Wait();
            
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(scopeFactory),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}