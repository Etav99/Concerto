using Concerto.Shared.Models.Dto;
using MudBlazor;

namespace Concerto.Client.Extensions;

public static class FolderContentItemTypeExtensions
{
	public static HashSet<string> DocumentExtensions = new() { ".doc", ".docx", ".odt", ".odg", ".ods", ".odp", ".xls", ".xlsx", ".ppt" , ".pptx", ".pdf", ".rtf", ".txt", ".wps", ".wpd", ".html" };
	public static HashSet<string> AudioExtensions = new() { ".mp3", ".wav", ".wma", ".ogg", ".flac", ".aac", ".m4a", ".aiff", ".au", ".raw", ".vox", ".m3u", ".wpl", ".m3u8", ".pls", ".mid", ".midi", ".rmi", ".kar", ".cda" };
	public static HashSet<string> ImageExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".jfif", ".tif", ".ico", ".svg", ".psd", ".ai", ".eps", ".indd", ".raw", ".nef", ".orf", ".sr2", ".srw", ".arw", ".cr2", ".crw", ".dng", ".erf", ".kdc", ".dcr", ".mdc", ".r3d", ".3fr", ".mef", ".pef", ".x3f", ".webp", ".jxr", ".hdp", ".wdp", ".jpm", ".jpx", ".heif", ".heic", ".avif" };
	public static HashSet<string> VideoExtensions = new() { ".mp4", ".avi", ".wmv", ".mov", ".flv", ".swf", ".vob", ".mng", ".qt", ".mpg", ".mpeg", ".3gp", ".mkv", ".webm", ".ogv", ".m4v", ".f4v", ".f4p", ".f4a", ".f4b" };
	public static HashSet<string> MusicSheetExtensions = new() { ".mscz", ".mscx", ".musicxml", ".mxl", ".md", ".cap", ".capx", ".bww", ".mgu", ".sgu", ".ove", ".scw", ".ptb" };
	public static string ToDisplayString(this FolderContentItem item)
	{
		if(item is FolderItem)
		{
			var type = ((FolderItem)item).Type;
			return FolderTypeExtensions.ToDisplayString(type);
		}
		else if(item is FileItem)
		{
			var extension = ((FileItem)item).Extension;

			return extension switch
			{
				_ when DocumentExtensions.Contains(extension) => "Document",
				_ when AudioExtensions.Contains(extension) => "Audio",
				_ when VideoExtensions.Contains(extension) => "Video",
				_ when ImageExtensions.Contains(extension) => "Image",
				_ when MusicSheetExtensions.Contains(extension) => "Music sheet",
				_ => "Other"
			};
		}
		return string.Empty;
	}

	public static string ToIcon(this FolderContentItem item)
	{
		if (item is FolderItem)
		{
			var type = ((FolderItem)item).Type;
			return type.ToIcon();
		}
		else if (item is FileItem)
		{
			var extension = ((FileItem)item).Extension;
			
			return extension switch
			{
				_ when DocumentExtensions.Contains(extension) => Icons.Custom.FileFormats.FileDocument,
				_ when AudioExtensions.Contains(extension) => @Icons.Custom.FileFormats.FileMusic,
				_ when ImageExtensions.Contains(extension) => @Icons.Custom.FileFormats.FileImage,
				_ when VideoExtensions.Contains(extension) => Icons.Custom.FileFormats.FileVideo,
				_ when MusicSheetExtensions.Contains(extension) => Icons.Custom.FileFormats.FileMusic,
				_ => @Icons.Filled.InsertDriveFile
			};
		}
		return string.Empty;
	}

	public static string ToIcon(this FolderType type)
	{
		return type switch
		{
			FolderType.CourseRoot => Icons.Filled.Home,
			FolderType.Sheets => Icons.Filled.MusicNote,
			FolderType.Recordings => Icons.Filled.VideoCameraFront,
			FolderType.Video => Icons.Filled.VideoLibrary,
			FolderType.Audio => Icons.Filled.LibraryMusic,
			FolderType.Documents => Icons.Filled.LibraryBooks,
			FolderType.Other => Icons.Filled.Workspaces,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

}
