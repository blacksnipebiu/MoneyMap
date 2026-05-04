# Bookkeeping - Avalonia 记账程序规划文档

> 版本: v1.0 | 日期: 2026-05-04 | 状态: 待执行

---

## 一、项目概述

### 1.1 目标

构建一个跨平台桌面记账应用，核心能力：

- **多源导入**：支付宝、微信支付、银行卡账单等不同来源的数据自动解析导入
- **统一汇总**：将不同格式的收支数据归一化到统一模型，消除格式差异
- **分析洞察**：提供分类统计、趋势分析、预算管理等多维度财务分析

### 1.2 核心用户场景

| 场景 | 描述 |
|------|------|
| 批量导入 | 用户下载支付宝/微信/银行CSV → 拖入程序 → 自动识别来源 → 解析入库 |
| 日常查阅 | 按时间/分类/来源筛选交易记录，搜索备注关键词 |
| 月度复盘 | 查看月度收支汇总、分类占比饼图、趋势折线图 |
| 分类管理 | 对未分类交易进行归类，自定义分类体系 |
| 数据导出 | 导出统一格式的CSV/Excel，便于二次分析 |

### 1.3 设计原则

- **格式后置**：先定义统一数据模型，具体来源的解析器可插拔扩展
- **数据不可变**：导入的原始记录只增不改，所有分析基于查询视图
- **本地优先**：所有数据存储在本地 SQLite，无需联网，隐私安全
- **渐进增强**：MVP 先跑通导入→查看→统计主流程，后续迭代高级功能

---

## 二、技术栈

| 层次 | 技术选型 | 说明 |
|------|---------|------|
| UI 框架 | **Avalonia UI 11.x** | 跨平台桌面框架，支持 Windows/macOS/Linux |
| MVVM 框架 | **CommunityToolkit.Mvvm 8.x** | 微软官方 MVVM 工具包，Source Generator 生成样板代码 |
| DI 容器 | **Microsoft.Extensions.DependencyInjection** | 标准依赖注入 |
| 数据库 | **SQLite** + **Entity Framework Core 8.x** | 轻量本地存储，Code-First 迁移 |
| CSV 解析 | **CsvHelper** | 高性能 CSV 读写，支持自定义映射 |
| Excel 解析 | **MiniExcel** 或 **ClosedXML** | 轻量 Excel 读取（银行账单常见 xlsx 格式） |
| 图表 | **LiveCharts2** 或 **ScottPlot 5** | Avalonia 兼容的图表库 |
| 日志 | **Serilog** | 结构化日志，文件输出 |
| 本地化 | **Avalonia 内置 resx** | 预留多语言支持，初期仅中文 |

---

## 三、项目结构

```
Bookkeeping/
├── Bookkeeping.sln
│
├── src/
│   ├── Bookkeeping.App/                        # Avalonia 主应用
│   │   ├── App.axaml / App.axaml.cs            # 应用入口，DI 配置
│   │   ├── Program.cs                          # 启动引导
│   │   ├── Views/                              # XAML 视图
│   │   │   ├── MainWindow.axaml               # 主窗口（导航壳）
│   │   │   ├── DashboardView.axaml            # 首页仪表盘
│   │   │   ├── TransactionsView.axaml         # 交易记录列表
│   │   │   ├── ImportView.axaml               # 导入向导
│   │   │   ├── AnalyticsView.axaml            # 分析报表
│   │   │   ├── CategoriesView.axaml           # 分类管理
│   │   │   └── SettingsView.axaml             # 设置
│   │   ├── ViewModels/                         # 视图模型
│   │   │   ├── MainWindowViewModel.cs
│   │   │   ├── DashboardViewModel.cs
│   │   │   ├── TransactionsViewModel.cs
│   │   │   ├── ImportViewModel.cs
│   │   │   ├── AnalyticsViewModel.cs
│   │   │   ├── CategoriesViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Converters/                         # XAML 值转换器
│   │   │   ├── AmountColorConverter.cs         # 收入绿/支出红
│   │   │   └── DateFormatConverter.cs
│   │   ├── Styles/                             # 全局样式
│   │   │   └── AppStyles.axaml
│   │   └── Assets/                             # 图标、字体等
│   │
│   ├── Bookkeeping.Core/                       # 核心业务层（纯 C#，无 UI 依赖）
│   │   ├── Models/                             # 领域模型
│   │   │   ├── Transaction.cs                  # 统一交易记录
│   │   │   ├── Category.cs                     # 分类
│   │   │   ├── Account.cs                      # 账户（数据来源）
│   │   │   ├── ImportRecord.cs                 # 导入记录（溯源）
│   │   │   └── Budget.cs                       # 预算（V2）
│   │   ├── Enums/                              # 枚举定义
│   │   │   ├── TransactionType.cs              # 收入/支出/转账
│   │   │   └── DataSource.cs                   # 支付宝/微信/银行卡
│   │   ├── Services/                           # 业务服务接口
│   │   │   ├── ITransactionService.cs
│   │   │   ├── ICategoryService.cs
│   │   │   ├── IImportService.cs
│   │   │   └── IAnalyticsService.cs
│   │   └── Interfaces/                         # 通用接口
│   │       └── IDateTimeProvider.cs
│   │
│   ├── Bookkeeping.Data/                       # 数据访问层
│   │   ├── BookkeepingDbContext.cs             # EF Core 上下文
│   │   ├── Migrations/                         # 数据库迁移
│   │   ├── Repositories/                       # 仓储实现
│   │   │   ├── TransactionRepository.cs
│   │   │   ├── CategoryRepository.cs
│   │   │   └── ImportRecordRepository.cs
│   │   ├── Configurations/                     # EF 实体配置
│   │   │   ├── TransactionConfiguration.cs
│   │   │   └── CategoryConfiguration.cs
│   │   └── ServiceCollectionExtensions.cs      # DI 注册扩展
│   │
│   └── Bookkeeping.Import/                     # 导入引擎（可插拔解析器）
│       ├── IRecordParser.cs                    # 解析器接口（策略模式）
│       ├── ParseResult.cs                      # 解析结果
│       ├── Parsers/                            # 各来源解析器实现
│       │   ├── AlipayParser.cs                 # 支付宝 CSV 解析
│       │   ├── WeChatPayParser.cs              # 微信支付 CSV 解析
│       │   └── BankStatementParser.cs          # 银行流水解析（基类+子类）
│       ├── ParserFactory.cs                    # 解析器工厂（自动识别来源）
│       ├── Detection/                          # 来源自动检测
│       │   ├── SourceDetector.cs               # 根据文件头/列名判断来源
│       │   └── SourceDetectionResult.cs
│       └── Mapping/                            # 字段映射配置
│           ├── AlipayFieldMapping.cs
│           ├── WeChatFieldMapping.cs
│           └── BankFieldMapping.cs
│
├── tests/
│   ├── Bookkeeping.Core.Tests/
│   ├── Bookkeeping.Data.Tests/
│   └── Bookkeeping.Import.Tests/               # 解析器单元测试（含样本数据）
│       └── SampleData/                         # 测试用 CSV 样本
│
└── docs/
    └── PLAN.md                                 # 本文档
```

---

## 四、核心数据模型

### 4.1 Transaction（统一交易记录）

```csharp
public class Transaction
{
    public long Id { get; set; }

    // === 核心字段 ===
    public TransactionType Type { get; set; }        // 收入 / 支出 / 转账
    public decimal Amount { get; set; }              // 金额（绝对值）
    public DateTime TransactionTime { get; set; }    // 交易时间
    public string? Counterparty { get; set; }        // 交易对方
    public string? Description { get; set; }         // 商品说明/备注
    public string? CategoryName { get; set; }        // 原始分类（来源自带）

    // === 归一化字段 ===
    public long? CategoryId { get; set; }            // 用户自定义分类（FK）
    public Category? Category { get; set; }

    // === 来源追踪 ===
    public DataSource Source { get; set; }           // 支付宝/微信/银行卡
    public long AccountId { get; set; }             // 所属账户
    public Account Account { get; set; }
    public long ImportRecordId { get; set; }        // 导入批次
    public ImportRecord ImportRecord { get; set; }

    // === 原始数据保留 ===
    public string? RawLine { get; set; }             // 原始行数据（JSON），用于溯源

    // === 元数据 ===
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // === 去重 ===
    public string? SourceTransactionId { get; set; } // 来源方交易流水号（去重键）
}
```

### 4.2 Category（分类体系）

```csharp
public class Category
{
    public long Id { get; set; }
    public string Name { get; set; }                // 分类名
    public string? Icon { get; set; }               // 图标标识
    public TransactionType Type { get; set; }       // 该分类适用于 收入/支出
    public long? ParentId { get; set; }             // 父分类（支持两级）
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; }
    public ICollection<Transaction> Transactions { get; set; }

    // === 规则 ===
    public string? AutoMatchPattern { get; set; }   // 自动归类正则/关键词
    public string? SourceCategoryMapping { get; set; } // JSON: {"支付宝": "餐饮美食", ...}
}
```

### 4.3 Account（账户/数据来源）

```csharp
public class Account
{
    public long Id { get; set; }
    public string Name { get; set; }                // "招行储蓄卡", "支付宝", "微信"
    public DataSource Source { get; set; }
    public string? LastFourDigits { get; set; }     // 卡号末四位
    public DateTime CreatedAt { get; set; }
}
```

### 4.4 ImportRecord（导入批次）

```csharp
public class ImportRecord
{
    public long Id { get; set; }
    public string FileName { get; set; }            // 原始文件名
    public DataSource Source { get; set; }          // 自动检测的来源
    public DateTime ImportTime { get; set; }
    public int TotalRows { get; set; }              // 文件总行数
    public int ImportedCount { get; set; }          // 成功导入数
    public int SkippedCount { get; set; }           // 跳过数（重复/无效）
    public int ErrorCount { get; set; }             // 解析错误数
    public string? ErrorDetails { get; set; }       // 错误详情（JSON）
}
```

### 4.5 枚举定义

```csharp
public enum TransactionType
{
    Income = 1,      // 收入
    Expense = 2,     // 支出
    Transfer = 3     // 转账（不计入收支）
}

public enum DataSource
{
    Alipay = 1,      // 支付宝
    WeChatPay = 2,   // 微信支付
    BankCard = 3,    // 银行卡
    Manual = 4,      // 手动录入
    Other = 99       // 其他（预留）
}
```

---

## 五、导入引擎设计（核心亮点）

### 5.1 架构：策略模式 + 工厂模式

```
用户拖入文件 → SourceDetector(自动识别来源) → ParserFactory(获取对应Parser)
                                                      │
                    ┌─────────────────────────────────┼──────────────────────┐
                    ▼                                ▼                      ▼
             AlipayParser                    WeChatParser            BankParser(基类)
              支付宝CSV                        微信CSV                各银行子类
                    │                                │                      │
                    ▼                                ▼                      ▼
             IRecordParser.Parse() → ParseResult (统一交易列表 + 错误信息 + 跳过信息)
                                                      │
                                                      ▼
             ImportService.Import()
               1. 去重（SourceTransactionId / 时间+金额+对方 组合键）
               2. 自动归类（Category.AutoMatchPattern 匹配）
               3. 批量写入 DB
               4. 记录 ImportRecord
```

### 5.2 IRecordParser 接口

```csharp
public interface IRecordParser
{
    /// <summary>支持的来源类型</summary>
    DataSource SupportedSource { get; }

    /// <summary>解析文件流为统一交易记录</summary>
    ParseResult Parse(Stream fileStream, string fileName);

    /// <summary>能否解析此文件（快速判断，读文件头前几行）</summary>
    bool CanParse(Stream fileStream, string fileName);
}
```

### 5.3 SourceDetector 来源自动检测

| 来源 | 检测策略 |
|------|---------|
| 支付宝 | 文件名含"支付宝"，CSV 首行含"交易号,商家订单号"等特征列 |
| 微信支付 | 文件名含"微信"，CSV 含"交易时间,交易类型,交易对方"等特征列 |
| 银行卡 | 文件名含银行名，或列名含"借方/贷方/余额"等银行术语 |
| 未知 | 提示用户手动选择来源 |

### 5.4 各来源格式参考（待实际数据确认）

> **以下为已知格式参考，需根据你实际的导出数据调整字段映射**

#### 支付宝 CSV（参考格式）

```
交易号, 商家订单号, 交易创建时间, 付款时间, 最近修改时间, 交易来源地, 类型,
交易对方, 商品名称, 金额（元）, 收/支, 交易状态, 服务费（元）, 成功退款（元）, 备注
```

- **编码**: UTF-8（新版）或 GBK（旧版）
- **特征**: 前 N 行可能有汇总信息（需跳过），数据行以交易号开头
- **金额**: 字符串，可能有千分位逗号，需清理

#### 微信支付 CSV（参考格式）

```
交易时间, 交易类型, 交易对方, 商品, 收/支, 金额(元), 支付方式, 当前状态,
交易单号, 商户单号, 备注
```

- **编码**: UTF-8
- **特征**: 前 16 行为账单概要（需跳过），数据行从第 17 行开始
- **金额**: 带货币符号 "¥"，需去除

#### 银行流水（各银行差异大）

- **格式**: CSV / Excel / PDF（PDF 暂不支持）
- **列名**: 各银行不统一，需逐家适配
- **方向标识**: 借方（支出）/ 贷方（收入），或用正负号区分

### 5.5 去重策略

```csharp
// 去重键生成（同一来源内）
string dedupKey = SourceTransactionId ?? $"{TransactionTime:yyyyMMddHHmmss}_{Amount}_{Counterparty}";
```

- 优先使用来源方流水号（最可靠）
- 无流水号时使用 `时间+金额+对方` 组合
- 同一 ImportRecord 批次内去重 + 跨批次去重

---

## 六、UI 布局规划

### 6.1 主窗口结构

```
+--------------------------------------------------------------+
|  Bookkeeping                              - [ ] X             |
+-------------+------------------------------------------------+
|             |                                                |
|  仪表盘      |    [主内容区 - 根据左侧导航切换]                  |
|             |                                                |
|  交易记录    |                                                |
|             |                                                |
|  导入数据    |                                                |
|             |                                                |
|  分析报表    |                                                |
|             |                                                |
|  分类管理    |                                                |
|             |                                                |
|  设置        |                                                |
|             |                                                |
+-------------+------------------------------------------------+
```

- 左侧：固定导航栏（SplitView / NavMenu），图标+文字
- 右侧：内容区，通过 ReactiveUI 或 CommunityToolkit 的 ObservableObject 切换 View

### 6.2 各页面功能

| 页面 | 核心组件 | 说明 |
|------|---------|------|
| 仪表盘 | 卡片（本月收入/支出/结余）+ 最近交易列表 + 小型趋势图 | 首页总览 |
| 交易记录 | DataGrid（可排序/筛选）+ 筛选栏（时间范围/分类/来源/关键词）+ 分页 | 核心数据浏览 |
| 导入数据 | 拖拽区 + 来源检测提示 + 预览表格 + 导入进度 + 结果报告 | 导入向导 |
| 分析报表 | 日期选择器 + 饼图（分类占比）+ 折线图（趋势）+ 柱状图（月度对比） | 财务分析 |
| 分类管理 | 树形列表（两级分类）+ 增删改 + 自动归类规则编辑 | 分类体系 |
| 设置 | 数据库路径 + 导出配置 + 分类预设重置 + 关于 | 基础设置 |

### 6.3 导入流程（关键交互）

```
1. 拖入文件 / 点击选择文件
       ↓
2. 自动检测来源 → 显示检测结果（"检测到: 支付宝账单"）
       ↓ (若不确定)
2'. 让用户手动选择来源
       ↓
3. 预览解析结果（表格展示前 10 条，标记异常行）
       ↓
4. 用户确认 → 执行导入
       ↓
5. 显示导入报告：
   - 成功: 156 条
   - 重复跳过: 23 条
   - 解析失败: 2 条（点击查看详情）
```

---

## 七、开发阶段规划

### Phase 1 - 骨架搭建（MVP 基础）

**目标**: 项目结构就位，能跑起来一个空壳

| 任务 | 产出 |
|------|------|
| 创建 Solution + 4 个项目 | Bookkeeping.sln + 项目引用关系 |
| 配置 DI 容器 | ServiceCollection 注册所有服务 |
| EF Core DbContext + 首次 Migration | 数据库能创建 |
| Avalonia 主窗口 + 导航框架 | 左侧菜单能切换空白页面 |
| 基础样式 | 统一配色、字体、间距 |

### Phase 2 - 数据导入核心

**目标**: 能把支付宝/微信 CSV 导进来

| 任务 | 产出 |
|------|------|
| 实现 IRecordParser 接口 + ParseResult | 解析器契约 |
| 实现 AlipayParser | 支付宝 CSV 解析（待实际数据调整映射） |
| 实现 WeChatPayParser | 微信 CSV 解析（待实际数据调整映射） |
| 实现 SourceDetector | 来源自动检测 |
| 实现 ParserFactory | 工厂模式串联 |
| 实现 ImportService | 去重 + 入库 + 记录 ImportRecord |
| 导入页面 UI | 拖拽/选择文件 → 预览 → 确认导入 → 报告 |

### Phase 3 - 交易查看与管理

**目标**: 能浏览、筛选、搜索已导入的交易

| 任务 | 产出 |
|------|------|
| TransactionService 查询/分页/筛选 | 业务层 |
| 交易记录页面 | DataGrid + 筛选栏 + 分页 |
| 分类体系 CRUD | CategoryService + 分类管理页面 |
| 自动归类 | 基于规则的交易自动分类 |
| 手动归类 | 右键/批量修改分类 |

### Phase 4 - 分析报表

**目标**: 可视化财务数据

| 任务 | 产出 |
|------|------|
| AnalyticsService 统计逻辑 | 月度汇总/分类占比/趋势 |
| 仪表盘页面 | 卡片 + 迷你图表 + 最近交易 |
| 分析报表页面 | 饼图 + 折线图 + 柱状图 |
| 日期范围筛选 | 本月/本季/本年/自定义 |

### Phase 5 - 增强功能（V2）

| 任务 | 产出 |
|------|------|
| 银行卡解析器 | 适配主流银行导出格式 |
| Excel 导入支持 | MiniExcel 读取 xlsx |
| 数据导出 | 导出统一 CSV / Excel |
| 手动录入交易 | 新增/编辑交易表单 |
| 预算管理 | 月度预算设置 + 超支提醒 |
| 多账户管理 | 账户间转账关联 |
| 数据备份/恢复 | 导出/导入 SQLite 数据库 |

---

## 八、关键风险与应对

| 风险 | 影响 | 应对 |
|------|------|------|
| 各来源格式差异大 | 解析器开发量大 | 策略模式隔离，每个来源独立迭代，不互相影响 |
| 银行格式不统一 | 银行解析器适配难 | 先支持 1-2 家常用银行，提供"自定义列映射"功能 |
| 编码问题 (GBK/UTF-8) | 中文乱码 | 自动检测编码（UTF8 BOM / GBK 探测），失败时让用户手动选 |
| 数据去重不准 | 重复导入或遗漏 | 优先用流水号去重；组合键去重时加容错（时间±1分钟） |
| 同一笔交易跨来源 | 支付宝付款但银行卡扣款 | Phase 5 处理，提供"合并关联"功能 |
| Avalonia 图表库成熟度 | 图表功能受限 | 选 LiveCharts2（Avalonia 原生支持）或 ScottPlot 做后备 |

---

## 九、待确认事项

> 以下事项需要你提供实际数据后才能推进：

| # | 事项 | 说明 |
|---|------|------|
| 1 | **支付宝导出文件样本** | 需要一份实际的支付宝账单 CSV，确认列名、编码、前导行数 |
| 2 | **微信导出文件样本** | 需要一份实际的微信账单 CSV，确认列名、前导行数 |
| 3 | **银行卡账单样本** | 哪家银行？CSV 还是 Excel？列名是什么？ |
| 4 | **分类体系偏好** | 使用支付宝/微信自带的分类，还是自定义？需要几级分类？ |
| 5 | **UI 风格偏好** | 简约白底？暗色主题？有无参考应用？ |
| 6 | **目标平台** | 仅 Windows？还是需要 macOS/Linux？ |
| 7 | **.NET 版本** | .NET 8（LTS）还是 .NET 9？ |

---

## 十、技术决策记录（ADR）

### ADR-1: 为什么选 SQLite + EF Core？

- **场景**: 单用户桌面应用，数据量预计 < 100万条
- **选项**: SQLite / LiteDB / JSON 文件
- **决策**: SQLite + EF Core
- **理由**: EF Core 生态成熟，迁移方便，LINQ 查询强大，未来如需换数据库成本低

### ADR-2: 为什么选 CommunityToolkit.Mvvm 而非 ReactiveUI？

- **场景**: MVVM 绑定框架
- **选项**: CommunityToolkit.Mvvm / ReactiveUI
- **决策**: CommunityToolkit.Mvvm
- **理由**: Source Generator 减少样板代码，学习曲线低，与 Avalonia 兼容好；ReactiveUI 响应式编程范式对团队有额外学习成本

### ADR-3: 为什么导入引擎独立成 Bookkeeping.Import 项目？

- **场景**: 解析器逻辑较复杂，且需要频繁扩展
- **选项**: 放在 Core / 独立项目
- **决策**: 独立项目
- **理由**: 解析器只依赖 CsvHelper/MiniExcel，不依赖 EF Core 和 UI；方便单元测试；新增来源只改此项目
