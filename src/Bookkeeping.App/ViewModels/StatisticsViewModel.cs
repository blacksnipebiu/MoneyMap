using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Bookkeeping.App.ViewModels;

public partial class StatisticsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<CategorySummaryDto> _categorySummaries = new();

    [ObservableProperty]
    private ObservableCollection<TopCategoryDto> _topCategories = new();

    [ObservableProperty]
    private ObservableCollection<MonthAmountDto> _trendData = new();

    [ObservableProperty]
    private TransactionType _selectedType = TransactionType.Expense;

    [ObservableProperty]
    private DateTime _selectedMonth;

    [ObservableProperty]
    private long? _selectedCategoryId;

    [ObservableProperty]
    private string? _selectedCategoryName;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private CategorySummaryDto? _selectedCategorySummary;

    // Chart series
    public ISeries[] PieChartSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] TrendChartSeries { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] TopNChartSeries { get; private set; } = Array.Empty<ISeries>();

    public Axis[] TrendXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] TrendYAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] TopNXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] TopNYAxes { get; private set; } = Array.Empty<Axis>();

    public StatisticsViewModel()
    {
        _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        InitializeChartAxes();
        _ = LoadAllDataAsync();
    }

    private void InitializeChartAxes()
    {
        TrendXAxes = new Axis[]
        {
            new Axis
            {
                Labels = Array.Empty<string>(),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        TrendYAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        TopNXAxes = new Axis[]
        {
            new Axis
            {
                Labels = Array.Empty<string>(),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        TopNYAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };
    }

    [RelayCommand]
    private async Task LoadSummaryAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var service = App.Services.GetRequiredService<IStatisticsService>();
            var result = await service.GetCategorySummaryAsync(SelectedMonth, SelectedType);
            CategorySummaries.Clear();
            foreach (var item in result)
            {
                CategorySummaries.Add(item);
            }
            UpdatePieChartSeries();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTopCategoriesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var service = App.Services.GetRequiredService<IStatisticsService>();
            var result = await service.GetTopCategoriesAsync(SelectedType, 5, SelectedMonth);
            TopCategories.Clear();
            foreach (var item in result)
            {
                TopCategories.Add(item);
            }
            UpdateTopNChartSeries();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTrendAsync()
    {
        if (!SelectedCategoryId.HasValue) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var service = App.Services.GetRequiredService<IStatisticsService>();
            var result = await service.GetCategoryTrendAsync(SelectedCategoryId.Value, 6);
            TrendData.Clear();
            foreach (var item in result.MonthlyData)
            {
                TrendData.Add(item);
            }
            UpdateTrendChartSeries();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadAllDataAsync()
    {
        await Task.WhenAll(
            LoadSummaryAsync(),
            LoadTopCategoriesAsync()
        );
    }

    [RelayCommand]
    private async Task SwitchTypeAsync(TransactionType type)
    {
        SelectedType = type;
        await LoadAllDataAsync();
    }

    [RelayCommand]
    private async Task ChangeMonthAsync(int offset)
    {
        SelectedMonth = SelectedMonth.AddMonths(offset);
        await LoadAllDataAsync();
    }

    partial void OnSelectedCategorySummaryChanged(CategorySummaryDto? value)
    {
        if (value != null)
        {
            SelectedCategoryId = value.CategoryId;
            SelectedCategoryName = value.CategoryName;
            _ = LoadTrendAsync();
        }
    }

    private void UpdatePieChartSeries()
    {
        if (!CategorySummaries.Any())
        {
            PieChartSeries = Array.Empty<ISeries>();
            return;
        }

        var colors = new SKColor[]
        {
            SKColor.Parse("#10B981"), // green
            SKColor.Parse("#3B82F6"), // blue
            SKColor.Parse("#F59E0B"), // yellow
            SKColor.Parse("#EF4444"), // red
            SKColor.Parse("#8B5CF6"), // purple
            SKColor.Parse("#EC4899"), // pink
            SKColor.Parse("#06B6D4"), // cyan
            SKColor.Parse("#84CC16")  // lime
        };

        var series = CategorySummaries.Select((item, index) => new PieSeries<decimal>
        {
            Values = new[] { item.TotalAmount },
            Name = item.CategoryName,
            Fill = new SolidColorPaint(colors[index % colors.Length]),
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            
        } as ISeries).ToArray();

        PieChartSeries = series;
        OnPropertyChanged(nameof(PieChartSeries));
    }

    private void UpdateTrendChartSeries()
    {
        if (!TrendData.Any())
        {
            TrendChartSeries = Array.Empty<ISeries>();
            OnPropertyChanged(nameof(TrendChartSeries));
            return;
        }

        var labels = TrendData.Select(d => $"{d.Year}/{d.Month:D2}").ToArray();
        TrendXAxes[0].Labels = labels;
        OnPropertyChanged(nameof(TrendXAxes));

        TrendChartSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Values = TrendData.Select(d => d.Amount).ToArray(),
                Name = SelectedCategoryName ?? "金额",
                Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#3B82F6")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White)
            }
        };
        OnPropertyChanged(nameof(TrendChartSeries));
    }

    private void UpdateTopNChartSeries()
    {
        if (!TopCategories.Any())
        {
            TopNChartSeries = Array.Empty<ISeries>();
            OnPropertyChanged(nameof(TopNChartSeries));
            return;
        }

        var labels = TopCategories.Select(c => c.CategoryName).ToArray();
        TopNXAxes[0].Labels = labels;
        OnPropertyChanged(nameof(TopNXAxes));

        TopNChartSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Values = TopCategories.Select(c => c.TotalAmount).ToArray(),
                Name = "金额",
                Fill = new SolidColorPaint(SKColor.Parse("#10B981")),
                MaxBarWidth = 40
            }
        };
        OnPropertyChanged(nameof(TopNChartSeries));
    }
}