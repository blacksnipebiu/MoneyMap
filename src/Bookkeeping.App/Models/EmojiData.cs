using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bookkeeping.App.Models;

/// <summary>
/// Static class containing emoji data organized by category.
/// </summary>
public static class EmojiData
{
    public static IReadOnlyList<EmojiItem> AllEmojis { get; }
    public static IReadOnlyList<string> Categories { get; }
    public static IReadOnlyList<EmojiItem> GetByCategory(string category) =>
        _categoryMap.TryGetValue(category, out var list) ? list : [];

    private static readonly Dictionary<string, IReadOnlyList<EmojiItem>> _categoryMap;

    static EmojiData()
    {
        var emojis = new List<EmojiItem>
        {
            // 餐饮 (Food & Dining) - 16 emojis
            new("🍔", "汉堡", "餐饮"),
            new("🍕", "披萨", "餐饮"),
            new("🍜", "面条", "餐饮"),
            new("🍝", "意面", "餐饮"),
            new("🍱", "便当", "餐饮"),
            new("🍣", "寿司", "餐饮"),
            new("🍩", "甜甜圈", "餐饮"),
            new("🍪", "饼干", "餐饮"),
            new("☕", "咖啡", "餐饮"),
            new("🧋", "奶茶", "餐饮"),
            new("🍵", "茶", "餐饮"),
            new("🍶", "清酒", "餐饮"),
            new("🍺", "啤酒", "餐饮"),
            new("🍷", "红酒", "餐饮"),
            new("🥤", "饮料", "餐饮"),
            new("🌮", "墨西哥卷饼", "餐饮"),

            // 交通 (Transportation) - 18 emojis
            new("🚗", "汽车", "交通"),
            new("🚕", "出租车", "交通"),
            new("🚙", "SUV", "交通"),
            new("🚌", "公交车", "交通"),
            new("🚎", "电车", "交通"),
            new("🏎️", "赛车", "交通"),
            new("🚓", "警车", "交通"),
            new("🚑", "救护车", "交通"),
            new("🚒", "消防车", "交通"),
            new("🚐", "货车", "交通"),
            new("🚚", "卡车", "交通"),
            new("🚛", "拖车", "交通"),
            new("🚜", "拖拉机", "交通"),
            new("🛴", "滑板车", "交通"),
            new("🚲", "自行车", "交通"),
            new("🛵", "摩托车", "交通"),
            new("🏍️", "摩托车", "交通"),
            new("✈️", "飞机", "交通"),
            new("🚂", "火车", "交通"),

            // 购物 (Shopping) - 16 emojis
            new("🛒", "购物车", "购物"),
            new("🛍️", "购物袋", "购物"),
            new("🛟", "救生圈", "购物"),
            new("💄", "化妆品", "购物"),
            new("👗", "连衣裙", "购物"),
            new("👠", "高跟鞋", "购物"),
            new("🧣", "围巾", "购物"),
            new("🧤", "手套", "购物"),
            new("🧥", "外套", "购物"),
            new("👚", "女装", "购物"),
            new("👖", "牛仔裤", "购物"),
            new("🩱", "泳衣", "购物"),
            new("👙", "比基尼", "购物"),
            new("🩴", "拖鞋", "购物"),
            new("👞", "皮鞋", "购物"),
            new("👟", "运动鞋", "购物"),

            // 金融 (Finance) - 15 emojis
            new("💰", "钱袋", "金融"),
            new("💵", "美元", "金融"),
            new("💴", "日元", "金融"),
            new("💶", "欧元", "金融"),
            new("💷", "英镑", "金融"),
            new("💳", "信用卡", "金融"),
            new("💸", "花钱", "金融"),
            new("💱", "货币兑换", "金融"),
            new("💹", "涨势", "金融"),
            new("🪙", "硬币", "金融"),
            new("💎", "钻石", "金融"),
            new("⚖️", "天平", "金融"),
            new("🏦", "银行", "金融"),
            new("🏧", "ATM", "金融"),
            new("💰", "钱包", "金融"),

            // 生活 (Living) - 17 emojis
            new("🏠", "房子", "生活"),
            new("🏡", "庭院", "生活"),
            new("🏢", "办公楼", "生活"),
            new("🏣", "邮局", "生活"),
            new("🏤", "邮政", "生活"),
            new("🏥", "医院", "生活"),
            new("🏨", "酒店", "生活"),
            new("🏩", "旅馆", "生活"),
            new("🏪", "便利店", "生活"),
            new("🏬", "商场", "生活"),
            new("🏭", "工厂", "生活"),
            new("🏯", "城堡", "生活"),
            new("🏰", "宫殿", "生活"),
            new("🗼", "东京塔", "生活"),
            new("🗽", "自由女神", "生活"),
            new("⛪", "教堂", "生活"),
            new("🕌", "清真寺", "生活"),

            // 娱乐 (Entertainment) - 15 emojis
            new("🎮", "游戏", "娱乐"),
            new("🎲", "骰子", "娱乐"),
            new("🎯", "飞镖", "娱乐"),
            new("🎳", "保龄球", "娱乐"),
            new("🎱", "桌球", "娱乐"),
            new("🎰", "老虎机", "娱乐"),
            new("🎭", "戏剧", "娱乐"),
            new("🎨", "绘画", "娱乐"),
            new("🎪", "马戏团", "娱乐"),
            new("🎬", "电影", "娱乐"),
            new("🎤", "KTV", "娱乐"),
            new("🎧", "音乐", "娱乐"),
            new("🎹", "钢琴", "娱乐"),
            new("🎸", "吉他", "娱乐"),
            new("🎺", "小号", "娱乐"),
            new("🎻", "小提琴", "娱乐"),

            // 健康 (Health) - 14 emojis
            new("🏥", "医院", "健康"),
            new("🩺", "听诊器", "健康"),
            new("💊", "药", "健康"),
            new("🩹", "创可贴", "健康"),
            new("🏨", "急救", "健康"),
            new("⚕️", "医疗", "健康"),
            new("🩼", "拐杖", "健康"),
            new("🦷", "牙齿", "健康"),
            new("🦴", "骨头", "健康"),
            new("🧬", "DNA", "健康"),
            new("🧪", "试管", "健康"),
            new("🧫", "培养皿", "健康"),
            new("💉", "注射", "健康"),
            new("🩸", "血液", "健康"),

            // 教育 (Education) - 12 emojis
            new("📚", "书籍", "教育"),
            new("📖", "阅读", "教育"),
            new("📝", "笔记", "教育"),
            new("✏️", "铅笔", "教育"),
            new("📒", "笔记本", "教育"),
            new("📓", "日记", "教育"),
            new("📕", "书本", "教育"),
            new("📗", "绿色书", "教育"),
            new("📘", "蓝色书", "教育"),
            new("📙", "橙色书", "教育"),
            new("🔬", "显微镜", "教育"),
            new("🧮", "算盘", "教育"),
            new("📐", "三角尺", "教育"),

            // 常用 (Common) - 16 emojis
            new("⭐", "星星", "常用"),
            new("🌟", "闪星", "常用"),
            new("✨", "闪光", "常用"),
            new("💫", "旋转星", "常用"),
            new("🔴", "红圆", "常用"),
            new("🟠", "橙圆", "常用"),
            new("🟡", "黄圆", "常用"),
            new("🟢", "绿圆", "常用"),
            new("🔵", "蓝圆", "常用"),
            new("🟣", "紫圆", "常用"),
            new("⚪", "白圆", "常用"),
            new("⚫", "黑圆", "常用"),
            new("🟤", "棕圆", "常用"),
            new("⬜", "白方", "常用"),
            new("🟫", "棕方", "常用"),
            new("📌", "图钉", "常用"),

            // 运动 (Sports) - 14 emojis
            new("⚽", "足球", "运动"),
            new("🏀", "篮球", "运动"),
            new("🏈", "橄榄球", "运动"),
            new("⚾", "棒球", "运动"),
            new("🎾", "网球", "运动"),
            new("🏐", "排球", "运动"),
            new("🏉", "橄榄球", "运动"),
            new("🥏", "飞盘", "运动"),
            new("🏓", "乒乓球", "运动"),
            new("🏸", "羽毛球", "运动"),
            new("🥊", "拳击", "运动"),
            new("🥋", "武术", "运动"),
            new("⛹️", "篮球", "运动"),
            new("🤸", "体操", "运动"),

            // 旅游 (Travel) - 15 emojis
            new("🏔️", "雪山", "旅游"),
            new("⛰️", "山峰", "旅游"),
            new("🌋", "火山", "旅游"),
            new("🗻", "富士山", "旅游"),
            new("🏕️", "露营", "旅游"),
            new("🏖️", "海滩", "旅游"),
            new("🏜️", "沙漠", "旅游"),
            new("🏝️", "岛屿", "旅游"),
            new("🏞️", "公园", "旅游"),
            new("🌅", "日出", "旅游"),
            new("🌄", "山景", "旅游"),
            new("🌠", "流星", "旅游"),
            new("🎢", "过山车", "旅游"),
            new("🎡", "摩天轮", "旅游"),
            new("🎠", "旋转木马", "旅游"),
        };

        AllEmojis = emojis.AsReadOnly();

        // Build category lookup
        _categoryMap = emojis
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EmojiItem>)g.ToList().AsReadOnly());

        Categories = _categoryMap.Keys.ToList().AsReadOnly();
    }
}