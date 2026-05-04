using Avalonia;
using Avalonia.Controls;

namespace Bookkeeping.App.Controls;

public partial class EmojiPickerButton : UserControl
{
    public static readonly StyledProperty<string> EmojiProperty =
        AvaloniaProperty.Register<EmojiPickerButton, string>(nameof(Emoji), string.Empty);

    public string Emoji
    {
        get => GetValue(EmojiProperty);
        set => SetValue(EmojiProperty, value);
    }

    public EmojiPickerButton()
    {
        InitializeComponent();
    }
}