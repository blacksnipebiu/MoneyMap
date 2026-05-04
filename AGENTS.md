# Bookkeeping - 跨平台桌面记账应用

## 项目结构

```
src/
├── Bookkeeping.Core/       # 核心模型、枚举、接口（无外部依赖）
├── Bookkeeping.Data/       # EF Core + SQLite 数据访问层
├── Bookkeeping.Import/     # 账单解析（支付宝CSV、微信Excel）
└── Bookkeeping.App/        # Avalonia UI 桌面应用
```

## 构建命令

```bash
# Release 构建（优先）
dotnet build src/Bookkeeping.App/Bookkeeping.App.csproj -c Release

# Debug 构建
dotnet build src/Bookkeeping.App/Bookkeeping.App.csproj

# 运行应用
dotnet run --project src/Bookkeeping.App/Bookkeeping.App.csproj
```

## 架构要点

### 依赖注入
- `App.Services` 静态属性暴露 `IServiceProvider`，ViewModel 通过它获取服务
- `App.ToastService` 静态属性暴露全局 Toast 服务
- 注册入口：`App.axaml.cs` 中的 `OnFrameworkInitializationCompleted()`

### 数据库
- SQLite，路径：`%LocalAppData%/Bookkeeping/bookkeeping.db`
- 使用 EF Core Code First，无迁移文件，通过 `EnsureCreatedAsync()` 初始化
- 重置数据库需调用 `SqliteConnection.ClearAllPools()` 释放连接池

### MVVM 模式
- ViewModel 缓存在 `MainWindowViewModel` 中，导航时复用实例（保留页面状态）
- ViewLocator 按命名约定自动匹配：`XxxViewModel` → `XxxView`
- 使用 CommunityToolkit.Mvvm 的 `[ObservableProperty]` 和 `[RelayCommand]`

### 账单导入
- `SourceDetector` 自动检测来源（支付宝/微信）
- `IRecordParser` 接口实现解析器，通过 `ParserFactory` 获取
- 导入流程：解析 → 预览（支持筛选、分页）→ 选择账户 → 确认导入
- `Transaction` 模型的 `AccountId` 和 `ImportRecordId` 是非空外键，导入时必须设置

### 筛选组件
- 使用 `FilterOption<T>` 类实现多选勾选筛选
- 筛选选项变化时通过 `OnChanged` 回调触发 `ApplyFilter()`
- 分页默认 20 条/页，可选 20/50/100/200

## 关键文件

| 文件 | 作用 |
|------|------|
| `App.axaml.cs` | DI 配置、数据库初始化、全局服务 |
| `MainWindowViewModel.cs` | ViewModel 缓存、页面导航 |
| `ServiceCollectionExtensions.cs` (Data) | DbContext 和 Repository 注册 |
| `ServiceCollectionExtensions.cs` (Import) | 解析器注册 |
| `Transaction.cs` | 核心交易模型，`AccountId` 和 `ImportRecordId` 是必填外键 |
| `FilterOption<T>` (ImportViewModel.cs) | 筛选选项包装类，支持勾选状态和变化回调 |

## Converters

在 `App.axaml` 中注册，XAML 中通过 `{StaticResource XxxConverter}` 引用：

| Converter | 用途 |
|-----------|------|
| `TransactionTypeBrushConverter` | 交易类型 → 画刷颜色 |
| `TransactionTypeNameConverter` | 交易类型 → 中文名称 |
| `TransactionTypeColorConverter` | 交易类型 → 颜色字符串 |
| `TransactionTypeAmountColorConverter` | 交易类型 → 金额颜色画刷 |
| `SelectionOpacityConverter` | 选中状态 → 透明度（1.0/0.5） |
| `DataSourceIconConverter` | 数据源 → 图标 |
| `DataSourceNameConverter` | 数据源 → 中文名称 |

## 注意事项

- Avalonia 12 的 DataGrid 需要单独引用 `Avalonia.Controls.DataGrid` 包（版本 12.0.0）
- 拖拽文件使用 `DataFormat.File` 和 `e.DataTransfer.TryGetFiles()`
- 文件对话框使用 `TopLevel.StorageProvider.OpenFilePickerAsync()`
- `Transaction` 模型的 `AccountId` 和 `ImportRecordId` 是非空外键，导入时必须设置
