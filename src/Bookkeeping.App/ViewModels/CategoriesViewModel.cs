using CommunityToolkit.Mvvm.ComponentModel;

namespace Bookkeeping.App.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _newCategoryName = "";
}