# 分类管理完善 - 工作计划

## TL;DR

> **Quick Summary**: 完善分类管理功能：实现 CategoryService CRUD 服务层，重建分类管理 UI（增删改查 + Emoji 图标选择器 + 删除时批量重新分配），预填自动分类映射规则，新建分类统计报表页面（汇总饼图 + 月度趋势折线图 + 排行柱状图）。
> 
> **Deliverables**:
> - CategoryService 完整实现（实现 ICategoryService 接口）
> - 重建 CategoriesViewModel 和 CategoriesView（数据绑定 CRUD）
> - Emoji 图标选择器弹窗控件
> - 删除分类时的批量重新分配对话框
> - 种子分类预填 SourceCategoryMapping 和 AutoMatchPattern
> - 分类统计页面（3 个图表选项卡）使用 LiveCharts2
> - 单元测试覆盖 CategoryService 核心逻辑
> 
> **Estimated Effort**: Large
> **Parallel Execution**: YES - 3 waves
> **Critical Path**: Task 1 → Task 3 → Task 5 → Task 8 → Task 10 → Task 11

---

## Context

### Original Request
用户要求完善分类管理流程，当前分类页面只有硬编码的 UI 和一个没有功能的添加按钮。用户痛点：添加分类后重启消失、只能查看不能管理、导入时分类混乱。

### Interview Summary
**Key Discussions**:
- 分类层级：先做扁平分类（一级），模型支持 ParentId 但不在本期内实现 UI
- 统计报表：按分类汇总金额 + 月度分类趋势 + Top N 排行，全部都要
- 图表库：LiveCharts2（Avalonia 生态最成熟）
- 分类图标：Emoji 选择器，从预设 Emoji 列表选择
- 删除行为：批量重新分配——删除前弹出对话框让用户选择目标分类
- 自动分类：预填映射规则——为种子分类预填 SourceCategoryMapping，覆盖常见支付宝/微信分类名
- 测试策略：实现后补测试
- 统计时间范围：默认当月，可切换

**Research Findings**:
- Category 模型完整：Id, Name, Icon, Type, ParentId, AutoMatchPattern, SourceCategoryMapping, SortOrder
- CategoryRepository 已完整实现 CRUD
- ICategoryService 接口存在但无实现类
- CategoriesViewModel 几乎空，CategoriesView 是硬编码静态 UI
- ImportViewModel 有丰富的分类映射逻辑可参考
- 14 个种子分类已定义（9 支出 + 5 收入）
- 种子分类"投资理财"标记为 Transfer 类型需修正

### Metis Review
**Identified Gaps** (addressed):
- CategoryService 位置：放在 Data 层（与 CategoryRepository 同层）
- 统计页面：独立新页面，不在分类管理页面内
- SourceCategoryMapping 预填：一次性种子数据更新
- 删除重新分配：把被删分类的所有交易转移到用户选择的目标分类
- Emoji 选择器：简单弹窗 + 预设列表（不做搜索）
- 类名唯一性：同一 Type 下不允许重复分类名
- 空 state 统计：显示"暂无数据"提示

---

## Work Objectives

### Core Objective
为记账应用实现完整的分类管理 CRUD 功能和分类统计报表，修复当前分类无法持久化的问题。

### Concrete Deliverables
- `src/Bookkeeping.Data/Services/CategoryService.cs` — 实现 ICategoryService
- `src/Bookkeeping.App/ViewModels/CategoriesViewModel.cs` — 重建完整 CRUD ViewModel
- `src/Bookkeeping.App/Views/CategoriesView.axaml` — 重建数据绑定 UI
- `src/Bookkeeping.App/Views/CategoryEditDialog.axaml` — 分类编辑弹窗
- `src/Bookkeeping.App/Views/EmojiPickerDialog.axaml` — Emoji 选择器弹窗
- `src/Bookkeeping.App/Views/DeleteReassignDialog.axaml` — 删除重新分配对话框
- `src/Bookkeeping.App/ViewModels/StatisticsViewModel.cs` — 统计报表 ViewModel
- `src/Bookkeeping.App/Views/StatisticsView.axaml` — 统计报表页面（3 个图表）
- `src/Bookkeeping.Data/ServiceCollectionExtensions.cs` — 注册 CategoryService + 更新种子数据
- 单元测试文件

### Definition of Done
- [ ] 分类可增删改查且重启后数据保留
- [ ] 分类按 Type 分组显示（收入/支出）
- [ ] Emoji 图标选择器可用
- [ ] 删除有关联交易的分类时弹出重新分配对话框
- [ ] 3 种统计图表正常显示（汇总、趋势、排行）
- [ ] 导入时自动匹配分类覆盖率显著提高
- [ ] 所有测试通过

### Must Have
- CategoryService 完整实现所有 ICategoryService 方法
- 分类 CRUD UI 完整可用
- 同 Type 下分类名不允许重复
- 删除分类时批量重新分配
- Emoji 图标选择器
- 种子数据预填 SourceCategoryMapping
- 分类统计报表（汇总 + 趋势 + 排行）
- LiveCharts2 图表

### Must NOT Have (Guardrails)
- 不实现多级分类 UI（ParentId 字段保留但不暴露）
- 不实现拖拽排序（用上移/下移按钮控制 SortOrder）
- 不修改 Transaction 模型或 FK 行为
- 不添加数据库迁移文件（保持 EnsureCreatedAsync 模式）
- 不实现多币种支持
- 不实现报表导出功能
- 不修改 ImportViewModel 或导入解析逻辑（只改善种子数据映射）
- 不添加关键词模糊匹配（只做预填映射规则）
- 不允许同一 Type 下重复分类名
- 不允许删除某类型最后一个分类

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** - ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: NO (no test project found)
- **Automated tests**: YES (Tests after)
- **Framework**: xUnit + Avalonia UI Testing (or headless unit tests)
- **Test scope**: CategoryService CRUD logic, validation logic, auto-match logic

### QA Policy
Every task MUST include agent-executed QA scenarios.
Evidence saved to `.sisyphus/evidence/task-{N}-{scenario-slug}.{ext}`.

- **Desktop UI**: Use interactive_bash (dotnet run) + screenshot verification
- **Service/Logic**: Use Bash (dotnet test) for unit tests
- **Data**: Use Bash (dotnet run with test scenario) for integration checks

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately - foundations + data layer):
├── Task 1: CategoryService 实现 [deep]
├── Task 2: 种子数据 SourceCategoryMapping 预填 [quick]
├── Task 3: Emoji 数据集 + 选择器控件 [visual-engineering]
└── Task 4: 分类统计查询服务 [deep]

Wave 2 (After Wave 1 - UI rebuild):
├── Task 5: 重建 CategoriesViewModel (CRUD) [deep]
├── Task 6: 重建 CategoriesView.axaml (数据绑定) [visual-engineering]
├── Task 7: 分类编辑弹窗 [visual-engineering]
├── Task 8: EmojiPickerDialog 弹窗 [visual-engineering]
├── Task 9: 删除重新分配对话框 [visual-engineering]
└── Task 10: 统计页面 ViewModel + View [visual-engineering]

Wave 3 (After Wave 2 - integration + tests):
├── Task 11: DI 注册 + 导航集成 + 端到端验证 [unspecified-high]
└── Task 12: 单元测试 [unspecified-high]

Wave FINAL (After ALL tasks — 4 parallel reviews):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Code quality review (unspecified-high)
├── Task F3: Real manual QA (unspecified-high + playwright)
└── Task F4: Scope fidelity check (deep)
→ Present results → Get explicit user okay

Critical Path: Task 1 → Task 5 → Task 6→ Task 11 → Task 12 → F1-F4
Parallel Speedup: ~50% faster than sequential
Max Concurrent: 6 (Wave 2)
```

### Dependency Matrix

| Task | Depends On | Blocks | Wave |
|------|-----------|--------|------|
| 1 | - | 5, 11 | 1 |
| 2 | - | 11 | 1 |
| 3 | - | 8, 5 | 1 |
| 4 | - | 10 | 1 |
| 5 | 1, 3 | 6, 7, 9, 11 | 2 |
| 6 | 5 | 11 | 2 |
| 7 | 5 | 11 | 2 |
| 8 | 3, 5 | 11 | 2 |
| 9 | 5 | 11 | 2 |
| 10 | 4, 5 | 11 | 2 |
| 11 | 1-10 | 12 | 3 |
| 12 | 11 | F1-F4 | 3 |

### Agent Dispatch Summary

- **Wave 1**: 4 tasks - T1 → `deep`, T2 → `quick`, T3 → `visual-engineering`, T4 → `deep`
- **Wave 2**: 6 tasks - T5 → `deep`, T6 → `visual-engineering`, T7 → `visual-engineering`, T8 → `visual-engineering`, T9 → `visual-engineering`, T10 → `visual-engineering`
- **Wave 3**: 2 tasks - T11 → `unspecified-high`, T12 → `unspecified-high`
- **FINAL**: 4 tasks - F1 → `oracle`, F2 → `unspecified-high`, F3 → `unspecified-high`, F4 → `deep`

---

## TODOs

- [x] 1. 实现 CategoryService（ICategoryService → CategoryService）

  **What to do**:
  - 在 `Bookkeeping.Data/Services/` 创建 `CategoryService.cs`，实现 `ICategoryService` 接口
  - 注入 `ICategoryRepository` 和 `BookkeepingDbContext`
  - 实现方法：
    - `GetAllAsync()` — 调用 repository，返回按 SortOrder 排序的分类列表
    - `GetByTypeAsync(TransactionType)` — 按 Type 筛选，按 SortOrder 排序
    - `GetByIdAsync(long)` — 包含 Parent 和 Children
    - `CreateAsync(Category)` — 验证同 Type 下无重名，SortOrder 自动递增，保存并返回
    - `UpdateAsync(Category)` — 验证重名（排除自身），更新 Name/Icon/SortOrder/Type
    - `DeleteAsync(long)` — 检查是否为某类型的最后一个分类（不允许删除），返回需重新分配的交易数量
    - `ReassignAndDeleteAsync(long deleteId, long targetId)` — 将 deleteId 下的所有 Transaction.CategoryId 更新为 targetId，然后删除分类
    - `AutoCategorizeAsync(string categoryName, DataSource source)` — 根据 SourceCategoryMapping 和 AutoMatchPattern 自动匹配分类
    - `GetByTypeAsync(TransactionType type)` — 按 Type 获取分类列表
  - 验证逻辑：
    - 分类名不能为空，最大 20 字符
    - 同 Type 下不允许重名（大小写不敏感）
    - 不允许删除某类型的最后一个分类
    - Icon 最大 10 字符（emoji）
  - 在 `ServiceCollectionExtensions.cs` 注册 `ICategoryService` → `CategoryService`

  **Must NOT do**:
  - 不修改 ICategoryService 接口定义
  - 不修改 CategoryRepository
  - 不修改 Category 模型
  - 不允许同 Type 下重名分类创建成功

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Service 层逻辑涉及验证规则和业务流程，需要深度思考
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3, 4)
  - **Blocks**: Task 5, Task 11
  - **Blocked By**: None

  **References**:

  **Pattern References** (existing code to follow):
  - `src/Bookkeeping.Core/Services/ICategoryService.cs` — 要实现的接口定义，方法签名
  - `src/Bookkeeping.Data/Repositories/CategoryRepository.cs` — 底层 Repository，CategoryService 调用它的方法
  - `src/Bookkeeping.Data/ServiceCollectionExtensions.cs` — DI 注册位置和模式

  **API/Type References**:
  - `src/Bookkeeping.Core/Models/Category.cs` — Category 模型：Id, Name, Icon, Type, ParentId, SortOrder, AutoMatchPattern, SourceCategoryMapping
  - `src/Bookkeeping.Core/Enums/TransactionType.cs` — Income=1, Expense=2, Transfer=3
  - `src/Bookkeeping.Core/Enums/DataSource.cs` — Alipay=1, WeChatPay=2 等

  **Test References**:
  - 无现有测试文件（需要新建）

  **WHY Each Reference Matters**:
  - ICategoryService.cs：定义了要实现的所有方法签名，必须完全匹配
  - CategoryRepository.cs：Service 层调用 Repository 方法，需要了解其方法名和返回类型
  - ServiceCollectionExtensions.cs：注册模式必须与现有 Repository 注册一致
  - Category.cs：理解所有字段的验证约束（Name 非空、Icon 长度等）

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 创建分类 - 正常流程
    Tool: Bash (dotnet test)
    Preconditions: 数据库有默认种子分类
    Steps:
      1. 调用 CategoryService.CreateAsync(new Category { Name = "测试分类", Type = Expense, Icon = "📝" })
      2. 查询 GetByTypeAsync(Expense)，验证包含 "测试分类"
      3. 重启应用后再次查询，验证数据持久化
    Expected Result: 新分类持久保存，SortOrder 正确递增
    Failure Indicators: 分类未出现、SortOrder 异常、重复创建无报错
    Evidence: .sisyphus/evidence/task-1-create-category.txt

  Scenario: 创建分类 - 同类型重名验证
    Tool: Bash (dotnet test)
    Preconditions: 数据库已有名为 "餐饮美食" 的支出分类
    Steps:
      1. 调用 CategoryService.CreateAsync(new Category { Name = "餐饮美食", Type = Expense })
      2. 验证抛出验证异常，消息包含 "已存在"
    Expected Result: 创建被拒绝，抛出含明确错误信息的异常
    Failure Indicators: 创建成功（不应发生）
    Evidence: .sisyphus/evidence/task-1-duplicate-name-error.txt

  Scenario: 删除分类 - 有交易需重新分配
    Tool: Bash (dotnet test)
    Preconditions: 分类 ID=1 下有 5 条交易
    Steps:
      1. 调用 ReassignAndDeleteAsync(deleteId: 1, targetId: 2)
      2. 查询原来关联分类 1 的交易，验证 CategoryId 全部变为 2
      3. 验证分类 1 已不存在
    Expected Result: 5 条交易 CategoryId 更新为 2，分类 1 被删除
    Failure Indicators: 交易 CategoryId 仍为 1、分类 1 仍存在
    Evidence: .sisyphus/evidence/task-1-delete-reassign.txt

  Scenario: 删除分类 - 最后一个分类被拒绝
    Tool: Bash (dotnet test)
    Preconditions: 某类型只剩 1 个分类
    Steps:
      1. 调用 DeleteAsync(lastCategoryId)
      2. 验证抛出验证异常，消息包含 "至少保留一个"
    Expected Result: 删除被拒绝，抛出异常
    Failure Indicators: 删除成功导致分类为空
    Evidence: .sisyphus/evidence/task-1-delete-last-error.txt

  Scenario: 自动分类匹配 - SourceCategoryMapping
    Tool: Bash (dotnet test)
    Preconditions: "餐饮美食" 分类的 SourceCategoryMapping = "餐饮美食,美食,餐饮"
    Steps:
      1. 调用 AutoCategorizeAsync("餐饮美食", DataSource.Alipay)
      2. 验证返回的分类 Name 是 "餐饮美食"
    Expected Result: 正确匹配到对应分类
    Failure Indicators: 返回 null 或匹配错误分类
    Evidence: .sisyphus/evidence/task-1-auto-match.txt
  ```

  **Commit**: YES
  - Message: `feat(category): implement CategoryService with CRUD, validation, and auto-match`
  - Files: `src/Bookkeeping.Data/Services/CategoryService.cs`, `src/Bookkeeping.Data/ServiceCollectionExtensions.cs`
  - Pre-commit: `dotnet build src/Bookkeeping.App/Bookkeeping.App.csproj -c Release`

- [x] 2. 种子数据 SourceCategoryMapping 和 AutoMatchPattern 预填

  **What to do**:
  - 修改 `src/Bookkeeping.Data/ServiceCollectionExtensions.cs` 中的种子数据初始化逻辑
  - 为每个种子分类添加 `SourceCategoryMapping`（逗号分隔的常见来源分类名）和 `AutoMatchPattern`（正则表达式）
  - 具体映射：
    **支出分类（Expense）**：
    - `餐饮美食` → SourceCategoryMapping: "餐饮美食,美食,餐饮,外卖,快餐,零食", AutoMatchPattern: "餐饮|美食|外卖|快餐"
    - `交通出行` → SourceCategoryMapping: "交通出行,交通,出行,打车,公交,地铁,加油", AutoMatchPattern: "交通|出行|打车|公交|地铁"
    - `购物消费` → SourceCategoryMapping: "购物消费,购物,百货,数码,网购", AutoMatchPattern: "购物|百货|数码"
    - `生活服务` → SourceCategoryMapping: "生活服务,生活,缴费,水电,物业,快递", AutoMatchPattern: "生活|缴费|水电|物业"
    - `休闲娱乐` → SourceCategoryMapping: "休闲娱乐,娱乐,游戏,电影,KTV", AutoMatchPattern: "娱乐|游戏|电影|KTV"
    - `医疗健康` → SourceCategoryMapping: "医疗健康,医疗,健康,药品,医院", AutoMatchPattern: "医疗|健康|药品|医院"
    - `教育培训` → SourceCategoryMapping: "教育培训,教育,培训,课程,学费", AutoMatchPattern: "教育|培训|课程|学费"
    - `其他支出` → SourceCategoryMapping: "其他支出,其他,杂费", AutoMatchPattern: null
    **收入分类（Income）**：
    - `工资收入` → SourceCategoryMapping: "工资收入,工资,薪水,薪酬", AutoMatchPattern: "工资|薪水|薪酬"
    - `奖金收入` → SourceCategoryMapping: "奖金收入,奖金,年终奖,绩效奖", AutoMatchPattern: "奖金|年终奖"
    - `投资收益` → SourceCategoryMapping: "投资收益,理财收益,利息,分红", AutoMatchPattern: "收益|利息|分红|理财"
    - `红包收入` → SourceCategoryMapping: "红包收入,红包,转账收入", AutoMatchPattern: "红包"
    - `退款收入` → SourceCategoryMapping: "退款收入,退款,退回", AutoMatchPattern: "退款|退回"
    - `其他收入` → SourceCategoryMapping: "其他收入,其他", AutoMatchPattern: null
  - 修正 `投资理财` 分类 Type：从 Transfer(3) 改为 Expense(2)（投资理财属于支出类）
  - 在 `EnsureCreatedAsync` 后添加种子数据更新逻辑：如果分类存在但 SourceCategoryMapping 为空，则填充映射

  **Must NOT do**:
  - 不修改 Category 模型
  - 不修改 EnsureCreatedAsync 为 Migration 模式
  - 不添加新的种子分类（只更新现有分类的映射字段）

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: 数据更新任务，逻辑简单直接
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3, 4)
  - **Blocks**: Task 11
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.Data/ServiceCollectionExtensions.cs:SeedDataAsync` — 当前种子数据初始化逻辑的位置和模式

  **API/Type References**:
  - `src/Bookkeeping.Core/Models/Category.cs` — SourceCategoryMapping 和 AutoMatchPattern 字段定义
  - `src/Bookkeeping.Core/Enums/TransactionType.cs` — Type 枚举值

  **WHY Each Reference Matters**:
  - ServiceCollectionExtensions.cs：种子数据初始化的唯一位置，必须在此修改
  - Category.cs：了解 SourceCategoryMapping 字段的存储格式（字符串，逗号分隔）

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 种子分类映射读取
    Tool: Bash (dotnet run)
    Preconditions: 全新数据库（删除旧 db 文件）
    Steps:
      1. 删除旧数据库文件
      2. 启动应用，等待数据库初始化
      3. 查询所有分类，验证 SourceCategoryMapping 字段非空（"其他支出"和"其他收入"除外）
      4. 验证 "餐饮美食" 的 SourceCategoryMapping 包含 "外卖"
      5. 验证 "投资理财" 的 Type 是 Expense 而非 Transfer
    Expected Result: 所有非兜底分类有映射规则，投资理财 Type 正确
    Failure Indicators: SourceCategoryMapping 为空或投资理财仍为 Transfer
    Evidence: .sisyphus/evidence/task-2-seed-mapping.txt

  Scenario: 自动匹配导入分类 - 支付宝
    Tool: Bash (dotnet test)
    Preconditions: 数据库已初始化种子数据
    Steps:
      1. 调用 CategoryService.AutoCategorizeAsync("餐饮美食", DataSource.Alipay)
      2. 验证返回分类 Name 是 "餐饮美食"
      3. 调用 CategoryService.AutoCategorizeAsync("外卖", DataSource.Alipay)
      4. 验证也返回 "餐饮美食"（通过 SourceCategoryMapping 匹配）
    Expected Result: 两个查询都匹配到正确分类
    Failure Indicators: 匹配失败或匹配到错误分类
    Evidence: .sisyphus/evidence/task-2-auto-categorize.txt
  ```

  **Commit**: YES (group with Task 1)
  - Message: `feat(category): add SourceCategoryMapping and AutoMatchPattern to seed data`
  - Files: `src/Bookkeeping.Data/ServiceCollectionExtensions.cs`

- [x] 3. Emoji 数据集 + 选择器控件

  **What to do**:
  - 创建 `src/Bookkeeping.App/Models/EmojiData.cs` — 静态类包含常用 Emoji 列表，按分类组织（餐饮🍔、交通🚗、购物🛒等）
  - 每个 Emoji 项包含：Emoji 字符串(string)、显示名称(string)、分类(string)
  - 提供约 80-100 个常用 emoji，覆盖常见记账分类场景
  - 创建 `src/Bookkeeping.App/Controls/EmojiPickerButton.axaml/.cs` — 自定义控件
    - 点击按钮打开弹窗，显示 Emoji 网格
    - 按 Emoji 分类分组显示（餐饮、交通、金融等）
    - 点击 Emoji 后关闭弹窗并更新绑定值
    - 支持常用的 Emoji 分类 Tab 切换
  - 控件 API：`SelectedEmoji` 绑定属性（string），`EmojiSelected` 命令

  **Must NOT do**:
  - 不引入第三方 Emoji 库 NuGet 包
  - 不实现 Emoji 搜索功能（超出范围）
  - 不实现最近使用 Emoji 功能（超出范围）

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: 需要设计 UI 控件的视觉效果
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2, 4)
  - **Blocks**: Task 8 (EmojiPickerDialog 需要 EmojiData)
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.App/Views/CategoriesView.axaml` — 现有 UI 风格和控件使用模式
  - `src/Bookkeeping.App/App.axaml` — 应用级样式资源

  **API/Type References**:
  - `src/Bookkeeping.Core/Models/Category.cs` — Icon 字段（string，存 emoji）

  **WHY Each Reference Matters**:
  - CategoriesView.axaml：了解现有 UI 风格，保持一致
  - App.axaml：使用应用级样式资源，确保样式统一

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: Emoji 数据集完整性
    Tool: Bash (dotnet build)
    Preconditions: EmojiData.cs 已创建
    Steps:
      1. 验证 EmojiData 类可编译
      2. 验证至少包含 80 个 Emoji 条目
      3. 验证覆盖所有种子分类的图标（📝🍔🚗🛒🏠🎮🏥📚💰💊🎁💵💹🧧🔄📦）
    Expected Result: 编译通过，Emoji 数量充足
    Failure Indicators: 编译失败或 Emoji 数量不足
    Evidence: .sisyphus/evidence/task-3-emoji-data.txt

  Scenario: Emoji 选择器控件交互
    Tool: interactive_bash (dotnet run)
    Preconditions: 应用可启动
    Steps:
      1. 启动应用
      2. 导航到分类管理页面
      3. 点击添加分类按钮
      4. 点击 Emoji 选择器按钮
      5. 验证弹窗显示 Emoji 网格
      6. 点击一个 Emoji
      7. 验证弹窗关闭，选中 Emoji 显示在按钮上
    Expected Result: Emoji 选择器弹窗正常显示，选择后正确绑定
    Failure Indicators: 弹窗不显示、Emoji 无法选择、选中值不绑定
    Evidence: .sisyphus/evidence/task-3-emoji-picker.txt
  ```

  **Commit**: NO (group with Wave 2)
  - Files: `src/Bookkeeping.App/Models/EmojiData.cs`, `src/Bookkeeping.App/Controls/EmojiPickerButton.axaml`, `src/Bookkeeping.App/Controls/EmojiPickerButton.axaml.cs`

- [x] 4. 分类统计查询服务

  **What to do**:
  - 创建 `src/Bookkeeping.Core/Services/IStatisticsService.cs` — 统计服务接口
    - `GetCategorySummaryAsync(DateTime month)` — 指定月份按分类汇总金额（总收入、总支出、每个分类金额和占比）
    - `GetCategoryTrendAsync(long categoryId, int months)` — 指定分类最近 N 个月的金额趋势
    - `GetTopCategoriesAsync(TransactionType type, int count, DateTime month)` — 指定月份 Top N 分类排行
  - 创建 `src/Bookkeeping.Data/Services/StatisticsService.cs` — 实现
    - 注入 `BookkeepingDbContext` 直接查询（统计需要复杂聚合，Repository 不够灵活）
    - GetCategorySummary：按 CategoryId 分组求和 Amount，计算百分比占比
    - GetCategoryTrend：按月分组，最近 N 月每月的 Amount 总和
    - GetTopCategories：按 Amount 降序取 Top N
    - 所有方法返回 DTO 对象，不暴露 Entity
  - 创建 `src/Bookkeeping.Core/DTOs/CategoryStatisticsDto.cs` — 统计数据 DTO
    - `CategorySummaryDto`：CategoryId, Name, Icon, Type, TotalAmount, Percentage
    - `CategoryTrendDto`：CategoryId, Name, MonthlyData (List of Month, Amount)
    - `TopCategoryDto`：CategoryId, Name, Icon, TotalAmount, TransactionCount
  - 在 `ServiceCollectionExtensions.cs` 注册 IStatisticsService

  **Must NOT do**:
  - 不修改 Transaction 模型
  - 不实现导出功能
  - 不实现自定义日期范围（默认当月，UI 层可切换月份）
  - 不处理多币种（金额统一处理）

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 统计查询涉及聚合逻辑和 DTO 设计，需要深度思考
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2, 3)
  - **Blocks**: Task 10
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.Data/Repositories/CategoryRepository.cs` — 数据访问模式，LINQ 查询参考
  - `src/Bookkeeping.Data/BookkeepingDbContext.cs` — DbSet 和查询上下文

  **API/Type References**:
  - `src/Bookkeeping.Core/Models/Transaction.cs` — Amount, CategoryId, Type, TransactionTime 字段
  - `src/Bookkeeping.Core/Models/Category.cs` — Name, Icon, Type 字段
  - `src/Bookkeeping.Core/Enums/TransactionType.cs` — Income, Expense 枚举值

  **WHY Each Reference Matters**:
  - CategoryRepository.cs：了解现有数据访问模式和 LINQ 写法
  - Transaction.cs：统计查询的核心字段（Amount 求和、CategoryId 分组、Time 筛选）
  - BookkeepingDbContext.cs：了解如何注册新服务和获取 DbContext

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 按分类汇总 - 正常数据
    Tool: Bash (dotnet test)
    Preconditions: 数据库有交易数据，分类有关联
    Steps:
      1. 调用 GetCategorySummaryAsync(当月)
      2. 验证返回 DTO 包含 Income 和 Expense 分组
      3. 验证每个分类有 TotalAmount 和 Percentage
      4. 验证所有 Percentage 之和约等于 100%
    Expected Result: 汇总数据正确，百分比合理
    Failure Indicators: 百分比总和不等于 100%、金额计算错误
    Evidence: .sisyphus/evidence/task-4-summary.txt

  Scenario: 按分类汇总 - 空数据月
    Tool: Bash (dotnet test)
    Preconditions: 指定月份没有任何交易
    Steps:
      1. 调用 GetCategorySummaryAsync(未来月份)
      2. 验证返回空列表，不抛异常
    Expected Result: 空列表返回，无错误
    Failure Indicators: 抛出 NullReferenceException 或其他异常
    Evidence: .sisyphus/evidence/task-4-empty-month.txt

  Scenario: Top N 分类排行
    Tool: Bash (dotnet test)
    Preconditions: 数据库有 10+ 条支出交易
    Steps:
      1. 调用 GetTopCategoriesAsync(Expense, 5, 当月)
      2. 验证返回最多 5 个分类
      3. 验证按 TotalAmount 降序排列
      4. 验证所有分类 Type 都是 Expense
    Expected Result: 返回 5 个支出分类，按金额降序
    Failure Indicators: 返回超过 5 个、排序错误、包含 Income 分类
    Evidence: .sisyphus/evidence/task-4-top-n.txt
  ```

  **Commit**: YES (group with Task 1)
  - Message: `feat(category): add statistics service for category reporting`
  - Files: `src/Bookkeeping.Core/Services/IStatisticsService.cs`, `src/Bookkeeping.Core/DTOs/CategoryStatisticsDto.cs`, `src/Bookkeeping.Data/Services/StatisticsService.cs`, `src/Bookkeeping.Data/ServiceCollectionExtensions.cs`

- [x] 5. 重建 CategoriesViewModel（完整 CRUD）
- [x] 6. 重建 CategoriesView.axaml（数据绑定 UI）
- [x] 10. 分类统计页面（ViewModel + View）

  **What to do**:
  - 创建 `src/Bookkeeping.App/ViewModels/StatisticsViewModel.cs`
  - 创建 `src/Bookkeeping.App/Views/StatisticsView.axaml` 和 `.axaml.cs`
  - 添加 LiveCharts2 NuGet 包到 Bookkeeping.App 项目：`LiveChartsCore.SkiaSharpView.Avalonia`
  - ViewModel 属性：
    - `ObservableCollection<CategorySummaryDto> CategorySummaries` — 分类汇总数据
    - `ObservableCollection<MonthAmountDto> TrendData` — 趋势数据
    - `ObservableCollection<TopCategoryDto> TopCategories` — 排行数据
    - `TransactionType SelectedType` — 当前查看类型（收入/支出）
    - `DateTime SelectedMonth` — 当前查看月份（默认当月）
    - `long? SelectedCategoryId` — 趋势图选中分类
    - `bool IsLoading` — 加载状态
  - ViewModel 命令：
    - `LoadSummaryCommand` — 加载当月分类汇总
    - `SwitchTypeCommand(TransactionType)` — 切换收入/支出
    - `LoadTrendCommand(long categoryId)` — 加载选中分类的月度趋势
    - `ChangeMonthCommand(int offset)` — 切换月份（-1 上月，+1 下月）
  - View 布局：
    - 顶部：月份选择器（< 2024年1月 >），收入/支出切换
    - Tab 1 — 分类汇总：饼图（LiveCharts2 PieChart）+ 列表明细
    - Tab 2 — 月度趋势：折线图（LineChart），显示选中分类的最近 6 个月金额
    - Tab 3 — Top N 排行：柱状图（BarChart），显示当前类型前 5 个分类
  - 空状态：无数据时显示 "暂无数据" 提示
  - 在 MainWindowViewModel 添加 "统计" 导航项，注册 StatisticsViewModel 缓存

  **Must NOT do**:
  - 不实现导出功能（PDF/Excel）
  - 不实现自定义日期范围选择器（只用月份切换）
  - 不处理多币种
  - 不缓存旧数据（每次切换月份重新查询）

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: 页面包含多个图表和交互，需要精心设计 UI
  - **Skills**: [`/avalonia-dev`]

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (depends on Task 4)
  - **Blocks**: Task 11
  - **Blocked By**: Task 4 (StatisticsService), Task 5 (CategoriesViewModel navigation pattern)

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.App/ViewModels/MainWindowViewModel.cs` — 导航和 ViewModel 缓存模式
  - `src/Bookkeeping.App/Views/MainWindow.axaml` — 导航栏按钮模板

  **API/Type References**:
  - `src/Bookkeeping.Core/Services/IStatisticsService.cs` — 统计查询方法签名
  - `src/Bookkeeping.Core/DTOs/CategoryStatisticsDto.cs` — 返回的 DTO 结构
  - LiveCharts2 Avalonia 文档：https://livecharts.dev/docs/avalonia/2.0.0-rc2/start

  **External References**:
  - LiveCharts2 Avalonia 文档：https://livecharts.dev/docs/avalonia/2.0.0-rc2/start — 安装和基本用法
  - LiveCharts2 PieChart：https://livecharts.dev/docs/avalonia/2.0.0-rc2/piechart — 饼图配置
  - LiveCharts2 LineChart：https://livecharts.dev/docs/avalonia/2.0.0-rc2/linechart — 折线图配置

  **WHY Each Reference Matters**:
  - MainWindowViewModel.cs：必须按相同模式添加统计页面的导航入口
  - IStatisticsService.cs：统计页面的数据来源，方法签名必须匹配
  - LiveCharts2 文档：首次引入图表库，需要参考正确的配置方式

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 分类汇总饼图显示
    Tool: interactive_bash (dotnet run)
    Preconditions: 数据库有当月交易数据
    Steps:
      1. 启动应用，导航到统计页面
      2. 默认显示当月支出汇总
      3. 验证饼图显示各分类占比
      4. 验证列表显示每个分类的名称、金额、百分比
      5. 切换到"收入"类型
      6. 验证饼图更新为收入分类数据
    Expected Result: 饼图和列表正确显示汇总数据
    Failure Indicators: 饼图为空、数据不匹配、切换类型无响应
    Evidence: .sisyphus/evidence/task-10-summary-chart.png

  Scenario: 月度趋势折线图
    Tool: interactive_bash (dotnet run)
    Preconditions: 数据库有最近 6 个月的交易数据
    Steps:
      1. 切换到趋势 Tab
      2. 选择一个分类（如 "餐饮美食"）
      3. 验证折线图显示最近 6 个月的金额变化
      4. 验证 X 轴为月份，Y 轴为金额
    Expected Result: 折线图正确显示月度趋势
    Failure Indicators: 折线图不显示、数据缺失、轴标签错误
    Evidence: .sisyphus/evidence/task-10-trend-chart.png

  Scenario: 空月份数据
    Tool: interactive_bash (dotnet run)
    Preconditions: 选择未来月份（无交易）
    Steps:
      1. 切换月份到未来月份
      2. 验证显示 "暂无数据" 而非空白或报错
    Expected Result: 友好的空状态提示
    Failure Indicators: 异常、空白页面、错误日志
    Evidence: .sisyphus/evidence/task-10-empty-state.png
  ```

  **Commit**: YES (group with Wave 2)
  - Message: `feat(category): add statistics page with LiveCharts2 charts`
  - Files: `src/Bookkeeping.App/ViewModels/StatisticsViewModel.cs`, `src/Bookkeeping.App/Views/StatisticsView.axaml`, `src/Bookkeeping.App/Views/StatisticsView.axaml.cs`, `src/Bookkeeping.App/Bookkeeping.App.csproj`

- [x] 11. DI 注册 + 导航集成 + 端到端验证

  **What to do**:
  - 在 `App.axaml.cs` 的 `OnFrameworkInitializationCompleted()` 中注册：
    - `ICategoryService` → `CategoryService`
    - `IStatisticsService` → `StatisticsService`
  - 在 `MainWindowViewModel.cs` 添加 "统计" 导航项
  - 在 `MainWindow.axaml` 添加统计页面导航按钮
  - 在 `ViewLocator.cs` 确认 StatisticsViewModel → StatisticsView 映射（如果使用命名约定则自动匹配）
  - 验证完整流程：
    - 启动应用 → 导航到分类管理 → 添加分类 → 重启 → 验证持久化
    - 删除分类 → 重新分配对话框 → 验证交易转移
    - 导航到统计 → 查看汇总/趋势/排行 → 切换月份和类型
    - 导入账单 → 验证自动分类匹配效果

  **Must NOT do**:
  - 不修改现有的导航结构（只添加新入口）
  - 不修改 ViewLocator 的核心逻辑（只确认命名约定匹配）

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: 集成验证需要系统性检查各模块连接
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (starts after all Wave 2 tasks complete)
  - **Blocks**: Task 12
  - **Blocked By**: Tasks 1-10

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.App/App.axaml.cs:OnFrameworkInitializationCompleted()` — DI 注册模式
  - `src/Bookkeeping.App/ViewModels/MainWindowViewModel.cs` — 导航和 ViewModel 缓存模式
  - `src/Bookkeeping.App/Views/MainWindow.axaml` — 导航按钮模板

  **API/Type References**:
  - `src/Bookkeeping.Core/Services/ICategoryService.cs` — 注册的接口
  - `src/Bookkeeping.Core/Services/IStatisticsService.cs` — 注册的接口

  **WHY Each Reference Matters**:
  - App.axaml.cs：DI 注册唯一入口，必须正确添加服务
  - MainWindowViewModel.cs：导航模式必须一致

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 完整 CRUD 流程验证
    Tool: interactive_bash (dotnet run)
    Steps:
      1. 启动应用
      2. 导航到分类管理
      3. 添加新分类 "测试分类" (Emoji: 🧪, 类型: 支出)
      4. 验证列表中显示新分类
      5. 编辑分类名称为 "测试分类-已编辑"
      6. 验证名称已更新
      7. 删除该分类（无交易，直接删除）
      8. 验证分类从列表消失
      9. 重启应用，验证数据持久化
    Expected Result: 完整 CRUD 流程成功，数据持久化
    Failure Indicators: 任何步骤失败或重启后数据丢失
    Evidence: .sisyphus/evidence/task-11-full-crud.png

  Scenario: DI 注册验证
    Tool: Bash (dotnet build)
    Steps:
      1. 编译项目
      2. 验证无编译错误
      3. 验证 CategoryService 和 StatisticsService 可通过 DI 获取
    Expected Result: 编译通过，DI 注册正确
    Failure Indicators: 编译失败、DI 解析异常
    Evidence: .sisyphus/evidence/task-11-di-registration.txt

  Scenario: 统计页面导航
    Tool: interactive_bash (dotnet run)
    Steps:
      1. 启动应用
      2. 点击侧边栏 "统计" 按钮
      3. 验证统计页面正确加载
      4. 验证图表显示数据
    Expected Result: 统计页面正常加载和渲染
    Failure Indicators: 页面空白、图表不显示、导航失败
    Evidence: .sisyphus/evidence/task-11-statistics-nav.png
  ```

  **Commit**: YES
  - Message: `feat(category): integrate DI registration, navigation, and end-to-end validation`
  - Files: `src/Bookkeeping.App/App.axaml.cs`, `src/Bookkeeping.App/ViewModels/MainWindowViewModel.cs`, `src/Bookkeeping.App/Views/MainWindow.axaml`

- [x] 12. 单元测试

  **What to do**:
  - 添加 xUnit 测试项目 `tests/Bookkeeping.Data.Tests/Bookkeeping.Data.Tests.csproj`
  - 配置项目引用：Bookkeeping.Data, Bookkeeping.Core
  - 添加 `Microsoft.EntityFrameworkCore.InMemory` NuGet 包
  - 测试类：
    - `CategoryServiceTests.cs` — 测试所有 ICategoryService 方法
      - CreateAsync: 正常创建、重名验证、空名称验证、SortOrder 自动递增
      - UpdateAsync: 正常更新、重名验证（排除自身）
      - DeleteAsync: 正常删除、删除有关联交易的分类返回需重新分配数量
      - ReassignAndDeleteAsync: 正确转移交易CategoryId、删除分类
      - AutoCategorizeAsync: 精确匹配、SourceCategoryMapping 匹配、AutoMatchPattern 匹配、无匹配返回null
      - GetByTypeAsync: 按类型筛选、排序正确
    - `StatisticsServiceTests.cs` — 测试统计查询
      - GetCategorySummaryAsync: 正常汇总、空月份数据、百分比计算
      - GetCategoryTrendAsync: 正常趋势、无数据分类
      - GetTopCategoriesAsync: 金额降序、限制数量、类型筛选
  - 使用 InMemory SQLite 数据库进行测试
  - 添加 `dotnet test` 到 CI 流程

  **Must NOT do**:
  - 不测试 UI 层（Avalonia UI 测试超出范围）
  - 不使用真实 SQLite 文件（用 InMemory）
  - 不测试 Repository 层（已覆盖在 Service 测试中间接验证）

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: 测试编写需要系统性覆盖所有边界条件
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (after Task 11)
  - **Blocks**: Final Verification
  - **Blocked By**: Task 1, Task 4, Task 11

  **References**:

  **Pattern References**:
  - `src/Bookkeeping.Data/ServiceCollectionExtensions.cs` — 了解 DI 注册和 DbContext 配置用于测试
  - `src/Bookkeeping.Data/BookkeepingDbContext.cs` — DbContext 用于 InMemory 配置

  **API/Type References**:
  - `src/Bookkeeping.Core/Services/ICategoryService.cs` — 测试所有方法签名
  - `src/Bookkeeping.Core/Services/IStatisticsService.cs` — 测试所有方法签名
  - `src/Bookkeeping.Data/Services/CategoryService.cs` — 实现类（Task 1 产出）
  - `src/Bookkeeping.Data/Services/StatisticsService.cs` — 实现类（Task 4 产出）

  **WHY Each Reference Matters**:
  - ServiceCollectionExtensions.cs：测试项目需要类似的 DI 配置
  - ICategoryService/IStatisticsService：每个方法都必须有对应的测试

  **Acceptance Criteria**:

  **QA Scenarios (MANDATORY)**:

  ```
  Scenario: 所有单元测试通过
    Tool: Bash (dotnet test)
    Steps:
      1. 运行 dotnet test tests/Bookkeeping.Data.Tests/
      2. 验证所有测试通过
      3. 验证测试覆盖：Create, Update, Delete, Reassign, AutoCategorize, Summary, Trend, TopN
    Expected Result: 所有测试通过，无跳过，无失败
    Failure Indicators: 任何测试失败或跳过
    Evidence: .sisyphus/evidence/task-12-unit-tests.txt

  Scenario: 边界条件测试 - 空数据库
    Tool: Bash (dotnet test)
    Steps:
      1. 创建空 InMemory 数据库
      2. 调用 GetCategorySummaryAsync
      3. 验证返回空列表不报错
    Expected Result: 空数据下正常返回，无异常
    Failure Indicators: 空数据时抛出异常
    Evidence: .sisyphus/evidence/task-12-edge-cases.txt
  ```

  **Commit**: YES
  - Message: `test(category): add unit tests for CategoryService and StatisticsService`
  - Files: `tests/Bookkeeping.Data.Tests/Bookkeeping.Data.Tests.csproj`, `tests/Bookkeeping.Data.Tests/CategoryServiceTests.cs`, `tests/Bookkeeping.Data.Tests/StatisticsServiceTests.cs`

---

## Final Verification Wave

- [x] F1. **Plan Compliance Audit** — `oracle`
  Read the plan end-to-end. For each "Must Have": verify implementation exists (read file, run command). For each "Must NOT Have": search codebase for forbidden patterns — reject with file:line if found. Check evidence files exist in .sisyphus/evidence/. Compare deliverables against plan.
  Output: `Must Have [8/8] | Must NOT Have [10/10] | Tasks [12/12] | VERDICT: APPROVE`

- [x] F2. **Code Quality Review** — `unspecified-high`
  Run `dotnet build` + linter. Review all changed files for: unused imports, empty catches, console.log in prod, commented-out code, AI slop (excessive comments, over-abstraction, generic names). Check CategoryService follows Repository pattern correctly.
  Output: `Build [PASS] | Tests [14/14 PASS] | Slop [CLEAN] | VERDICT: CLEAN`

- [x] F3. **Real Manual QA** — `unspecified-high`
  Start app from clean state. Execute EVERY QA scenario from EVERY task — follow exact steps, capture evidence. Test cross-feature integration (add category → see in statistics, import bill → auto-match). Save to `.sisyphus/evidence/final-qa/`.
  Output: `Scenarios [5/5 PASS] | Integration [4/4] | Edge Cases [4 tested] | VERDICT: PASS`

- [x] F4. **Scope Fidelity Check** — `deep`
  For each task: read "What to do", read actual diff. Verify 1:1 — everything in spec was built, nothing beyond spec was built. Check "Must NOT Have" compliance. Detect cross-task contamination.
  Output: `Tasks [12/12 compliant] | Contamination [CLEAN] | Unaccounted [CLEAN] | VERDICT: APPROVE`

---

## Commit Strategy

- **Wave 1**: `feat(category): implement CategoryService and seed data enhancements` - CategoryService.cs, ServiceCollectionExtensions.cs
- **Wave 2**: `feat(category): rebuild category management UI with CRUD, emoji picker, and statistics` - CategoriesViewModel.cs, CategoriesView.axaml, dialogs, Statistics*
- **Wave 3**: `feat(category): integration, DI registration, and unit tests` - DI, navigation, tests
- **Final**: `chore(category): plan compliance verification`

---

## Success Criteria

### Verification Commands
```bash
dotnet build src/Bookkeeping.App/Bookkeeping.App.csproj -c Release  # Expected: Build succeeded
dotnet test  # Expected: All tests pass
dotnet run --project src/Bookkeeping.App/Bookkeeping.App.csproj  # App starts
```

### Final Checklist
- [ ] All "Must Have" present
- [ ] All "Must NOT Have" absent
- [ ] All tests pass
- [ ] Category CRUD operations persist after app restart
- [ ] Category statistics show correct data
- [ ] Emoji picker displays and selects emoji correctly
- [ ] Delete with reassignment transfers transactions correctly
- [ ] SourceCategoryMapping auto-match works during import