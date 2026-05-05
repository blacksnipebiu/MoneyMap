using System.Text.Json.Serialization;

namespace MoneyMap.App.Models;

/// <summary>
/// Represents a single emoji entry with its display name and category.
/// </summary>
public class EmojiItem
{
    [JsonPropertyName("emoji")]
    public string Emoji { get; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; }

    [JsonPropertyName("category")]
    public string Category { get; }

    public EmojiItem(string emoji, string displayName, string category)
    {
        Emoji = emoji;
        DisplayName = displayName;
        Category = category;
    }
}