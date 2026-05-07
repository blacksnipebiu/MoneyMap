# MoneyMap.App - Avalonia UI 桌面应用

**入口项目**。MVVM 表示层 + Avalonia 样式/控件 + 全局服务。

## STRUCTURE

```
App/
├── Views/Pages/          # 8 个页面 View（命名约定 ImportView ↔ ImportViewModel）
├── Views/Dialogs/         # 弹窗（EmojiPickerDialog, CategoryEditDialog）
├── ViewModels/Pages/      # 8 个页面 ViewModel（ImportViewModel 最大 2000+ 行）
│   └── Models/            # 映射 UI 模型（MappingModels.cs 230 行）
├── ViewModels/Dialogs/    # 弹窗 ViewModel
├── Converters/            # 15 个 IValueConverter
├── Controls/              # ToastContainer 等自定义控件
├── Services/              # ToastService（全局静态）
├── Styles/                # Colors → Typography → Controls 分层
├── App.axaml              # ViewLocator 注册、15 个转换器注册
└── App.axaml.cs           # DI 容器、DB 初始化、MainWindow 创建
```

## WHERE TO LOOK

| 任务 | 位置 | 说明 |
|------|------|------|
| **导入全流程** | `ImportViewModel.cs` | 2000+ 行，检测→解析→预览→映射弹窗→确认→入库 |
| **分类管理** | `CategoriesViewModel.cs` | 分类 CRUD、树形结构、拖拽排序、删除重分配 |
| **交易列表** | `TransactionsViewModel.cs` | 交易筛选、删除 |
| **统计** | `StatisticsViewModel.cs` | 按日期/类型/账户聚合查询 |
| **导航** | `MainWindowViewModel.cs` | 缓存 8 个 ViewModel、`NavigateTo()` 切换 |
| **Toast 通知** | `App.ToastService` | 全局静态 `ShowError/ShowSuccess/ShowWarning` |
| **转换器** | `Converters/` | 类型→中文、类型→颜色、数据源→图标等 |

## ImportViewModel 结构

最大文件（2000+ 行），包含两个内部类 + 完整导入管道：

| 区域 | 行号 | 说明 |
|------|------|------|
| `FilterOption<T>` | 28-55 | 多选筛选组件（勾选状态 + OnChanged 回调） |
| `TransactionItem` | 60-101 | 交易包装类（选择状态 + 映射状态） |
| 文件选择 | ~490 | 拖拽/选择、格式验证、来源检测 |
| 解析 | ~580 | SourceDetector → ParserFactory → IRecordParser |
| 映射弹窗 | ~1120 | 分类映射（类型筛选）+ 字段映射 + 支付方式映射 |
| 预览筛选 | ~1350 | 类型/状态/支付方式多选筛选 + 关键词搜索 + 分页 |
| 确认导入 | ~1750 | 映射完整性验证 → ImportOrchestrationService |
| 历史记录 | ~1910 | 导入历史 Load/Delete/双击重新解析 |

## CONVENTIONS (THIS MODULE)

- **DB 访问**: 每个 ViewModel 接收 `IServiceScopeFactory`，每次 DB 操作 `using var scope = _scopeFactory.CreateScope()` 创建新作用域
- **Toast**: 全局 `App.ToastService.ShowError(msg)` 静态调用，不注入
- **ViewModel 缓存**: `MainWindowViewModel` 构造时创建所有页面 ViewModel，导航时返回缓存实例（保留状态）
- **预览表格**: 使用 `ListBox + 列 Grid ItemTemplate` 呈现，不使用 DataGrid — 更灵活的自定义样式控制
- **映射弹窗**: 映射设置作为 overlay 弹窗（`IsMappingDialogOpen`），解析后直接进入预览
- **Converters**: 在 `App.axaml` Resources 注册，XAML 用 `{StaticResource XxxConverter}` 引用

## ANTI-PATTERNS

- **async void**: 仅限 `OnBecameCurrent()` 等生命周期回调，不用于命令
- **ImportViewModel 过大**: 2000+ 行，应拆分内部类到独立文件
- **DashboardViewModel / AnalyticsViewModel**: 12 行占位类，待实现
- **空 catch**: `LoadImportHistoryAsync` 和 `ImportRecord` 持久化中的 `catch { }`

## NOTES

- 21 个 `[ObservableProperty]` 属性 → 21 × 2 个 partial void 方法（OnXxxChanged/OnXxxChanging）
- 15 个 `[RelayCommand]` 命令 → 15 个 Command 属性
- `FilteredUnmappedCategories` / `FilteredMappedCategories` 随 `CategoryTypeFilter` 联动筛选
- 支付方式智能分组: `PaymentMethodHelper.ExtractRoot()` 提取 `&/(/（` 前的根名称
- 页面导航时 `OnBecameCurrent()` 自动调用刷新历史记录
