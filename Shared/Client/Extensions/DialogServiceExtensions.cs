using Concerto.Shared.Client.Components.Dialogs;
using MudBlazor;

namespace Concerto.Shared.Client.Extensions;

public static class DialogServiceExtensions
{
	public static async Task <bool> ShowDeleteConfirmationDialog(this IDialogService dialogService, string title, string itemType, string itemName, bool additionalConfirmation = false)
	{
        var item = string.IsNullOrEmpty(itemType) ? itemName : $"{itemType} '{itemName}'";
        var text = $"Are you sure you want to delete {item}?";
        var parameters = new DialogParameters { ["Text"] = text, ["Confirmation"] = additionalConfirmation };
		DialogResult result = await dialogService.Show<ConfirmationDialog>(title, parameters).Result;
		if (result.Cancelled) return false;
		return true;
	}
    
    public static async Task<bool> ShowDeleteManyConfirmationDialog(this IDialogService dialogService, string title, string category, string items, bool additionalConfirmation = false)
    {
        var text = $"Are you sure you want to delete below {category}?\n\n{items}";
        var parameters = new DialogParameters { ["Text"] = text, ["Confirmation"] = additionalConfirmation };
        DialogResult result = await dialogService.Show<ConfirmationDialog>(title, parameters).Result;
        if (result.Cancelled) return false;
        return true;
    }



    public static async Task ShowInfoDialog(this IDialogService dialogService, string title, string text)
	{
		var parameters = new DialogParameters { ["Text"] = text};
		DialogResult result = await dialogService.Show<InfoDialog>(title, parameters).Result;
	}

}
