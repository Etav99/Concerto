using Concerto.Client.UI.Components.Dialogs;
using Concerto.Shared.Models.Dto;
using MudBlazor;

namespace Concerto.Client.Extensions;

public static class DialogServiceExtensions
{
    public static async Task<bool> ShowConfirmationDialog(
        this IDialogService dialogService,
        string title,
        string action,
        string itemType,
        string itemName,
        bool additionalConfirmation = false
    )
    {
        var item = string.IsNullOrEmpty(itemType) ? itemName : $"{itemType} '{itemName}'";
        var text = $"Are you sure you want to {action} {item}?";
        var parameters = new DialogParameters { ["Text"] = text, ["Confirmation"] = additionalConfirmation };
        var result = await dialogService.Show<ConfirmationDialog>(title, parameters).Result;
        if (result.Canceled) return false;
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
        if (result.Canceled) return false;
        return true;
    }


    public static async Task ShowInfoDialog(this IDialogService dialogService, string title, string text)
    {
        var parameters = new DialogParameters { ["Text"] = text };
        await dialogService.Show<InfoDialog>(title, parameters).Result;
    }

    public static async Task<long> ShowCreateCourseDialog(this IDialogService dialogService)
    {
        // var options = new DialogOptions() { FullScreen = true,  };
        var result = await dialogService.Show<CreateCourseDialog>("Create new course").Result;
        if (result.Canceled) return -1;
        return (long)result.Data;
    }

    public static async Task<FolderItem?> ShowSelectFolderDialog(this IDialogService dialogService, string title, string selectButtonText, long? initialCourseId = null, IEnumerable<long>? excludedIds = null, IEnumerable<long>? excludedWithChildrenIds = null)
    {
		var parameters = new DialogParameters
        {
			["ExcludedIds"] = excludedIds,
			["ExcludedWithChildrenIds"] = excludedWithChildrenIds,
            ["InitialCourseId"] = initialCourseId,
			["SelectButtonText"] = selectButtonText,
		};
		var result = await dialogService.Show<SelectFolderDialog>(title, parameters).Result;
		if (result.Canceled) return null;
		return (FolderItem)result.Data;
    }

    public static async Task<bool> ShowSelectFilesDialog(this IDialogService dialogService, HashSet<FileItem> selectedFiles, long courseId)
    {
        var options = new DialogOptions() { FullScreen = true, MaxWidth = MaxWidth.Large };
        var parameters = new DialogParameters
        {
            ["SelectedFiles"] = selectedFiles,
            ["CourseId"] = courseId
        };
        var result = await dialogService.Show<SelectFilesDialog>("Select files", parameters, options).Result;
        if (result.Canceled) return false;
        return true;
    }

    public static async Task<bool> ShowPostsRelatedToFileDialog(this IDialogService dialogService, long courseId, FileItem file)
    {
        var options = new DialogOptions() { FullScreen = true, MaxWidth = MaxWidth.Large };
        var parameters = new DialogParameters
        {
            ["File"] = file,
            ["CourseId"] = courseId
        };
        var result = await dialogService.Show<PostsRelatedToFileDialog>($"Posts related to {file.FullName}", parameters, options).Result;
        if (result.Canceled) return false;
        return true;
    }

    public static async Task<string?> ShowInputStringDialog(this IDialogService dialogService, string title, string placeholder)
    {
        var parameters = new DialogParameters
        {
            ["Placeholder"] = placeholder
        };
        var result = await dialogService.Show<InputStringDialog>(title, parameters).Result;
        if (result.Canceled) return null;
        return (string)result.Data;
    }

    public static async Task<bool> ShowUpdateUserDialog(this IDialogService dialogService, UserIdentity user)
    {
        var parameters = new DialogParameters
        {
            ["User"] = user
        };
        var result = await dialogService.Show<UpdateUserDialog>($"Update user", parameters).Result;
        if (result.Canceled) return false;
        return true;
    }


    public static async Task ShowFilePreviewDialog(this IDialogService dialogService, FileItem file)
    {
        var parameters = new DialogParameters
        {
            ["File"] = file
        };
        await dialogService.Show<FilePreviewDialog>(file.FullName, parameters).Result;
    }

}



