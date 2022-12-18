using Concerto.Shared.Client.Components.Dialogs;
using Concerto.Shared.Models.Dto;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace Concerto.Shared.Client.Extensions;

public static class DialogServiceExtensions
{
	public static async Task<bool> ShowDeleteConfirmationDialog(
		this IDialogService dialogService,
		string title,
		string itemType,
		string itemName,
		bool additionalConfirmation = false
	)
	{
		var item = string.IsNullOrEmpty(itemType) ? itemName : $"{itemType} '{itemName}'";
		var text = $"Are you sure you want to delete {item}?";
		var parameters = new DialogParameters { ["Text"] = text, ["Confirmation"] = additionalConfirmation };
		var result = await dialogService.Show<ConfirmationDialog>(title, parameters).Result;
		if (result.Cancelled) return false;
		return true;
	}

	public static async Task<bool> ShowDeleteManyConfirmationDialog(
		this IDialogService dialogService,
		string title,
		string category,
		string items,
		bool additionalConfirmation = false
	)
	{
		var text = $"Are you sure you want to delete below {category}?\n\n{items}";
		var parameters = new DialogParameters { ["Text"] = text, ["Confirmation"] = additionalConfirmation };
		var result = await dialogService.Show<ConfirmationDialog>(title, parameters).Result;
		if (result.Cancelled) return false;
		return true;
	}


	public static async Task ShowInfoDialog(this IDialogService dialogService, string title, string text)
	{
		var parameters = new DialogParameters { ["Text"] = text };
		await dialogService.Show<InfoDialog>(title, parameters).Result;
	}

	public static async Task<long> ShowCreateCourseDialog(this IDialogService dialogService)
	{
		var result = await dialogService.Show<CreateCourseDialog>("Create new course").Result;
		if (result.Cancelled) return -1;
		return (long)result.Data;
	}
	

	public static async Task<bool> ShowCopyFolderItemsDialog(this IDialogService dialogService,IEnumerable<FolderContentItem> items, long fromFolderId, long initialCourseId)
	{
		var options = new DialogOptions() { FullScreen = true };
		var parameters = new DialogParameters
		{
			["Items"] = items,
			["FromFolderId"] = fromFolderId,
			["InitialCourseId"] = initialCourseId,
			["Copy"] = true
		};
		var result = await dialogService.Show<MoveOrCopyFolderDialog>("Copy selected items", parameters, options).Result;
		if (result.Cancelled) return false;
		return true;
	}

	public static async Task<bool> ShowMoveFolderItemsDialog(this IDialogService dialogService,IEnumerable<FolderContentItem> items, long fromFolderId, long initialCourseId)
	{
		var options = new DialogOptions() { FullScreen = true };
		var parameters = new DialogParameters
		{
			["Items"] = items,
			["FromFolderId"] = fromFolderId,
			["InitialCourseId"] = initialCourseId,
			["Copy"] = false
		};
		var result = await dialogService.Show<MoveOrCopyFolderDialog>("Move selected items", parameters, options).Result;
		if (result.Cancelled) return false;
		return true;
	}

}



