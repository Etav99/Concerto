using Concerto.Client.Extensions;
using Concerto.Shared.Models.Dto;
using MudBlazor;

namespace Concerto.Client.Extensions;

public static class FolderContentItemExtensions
{
    public static HashSet<string> DocumentExtensions = new()
    {
        ".doc",
        ".docx",
        ".odt",
        ".odg",
        ".ods",
        ".odp",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".pdf",
        ".rtf",
        ".txt",
        ".wps",
        ".wpd",
        ".html"
    };

    public static HashSet<string> AudioExtensions = new()
    {
        ".mp3",
        ".wav",
        ".wma",
        ".ogg",
        ".flac",
        ".aac",
        ".m4a",
        ".aiff",
        ".au",
        ".raw",
        ".vox",
        ".m3u",
        ".wpl",
        ".m3u8",
        ".pls",
        ".mid",
        ".midi",
        ".rmi",
        ".kar",
        ".cda"
    };

    public static HashSet<string> ImageExtensions = new()
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".tiff",
        ".jfif",
        ".tif",
        ".ico",
        ".svg",
        ".psd",
        ".ai",
        ".eps",
        ".indd",
        ".raw",
        ".nef",
        ".orf",
        ".sr2",
        ".srw",
        ".arw",
        ".cr2",
        ".crw",
        ".dng",
        ".erf",
        ".kdc",
        ".dcr",
        ".mdc",
        ".r3d",
        ".3fr",
        ".mef",
        ".pef",
        ".x3f",
        ".webp",
        ".jxr",
        ".hdp",
        ".wdp",
        ".jpm",
        ".jpx",
        ".heif",
        ".heic",
        ".avif"
    };

    public static HashSet<string> VideoExtensions = new()
    {
        ".mp4",
        ".avi",
        ".wmv",
        ".mov",
        ".flv",
        ".swf",
        ".vob",
        ".mng",
        ".qt",
        ".mpg",
        ".mpeg",
        ".3gp",
        ".mkv",
        ".webm",
        ".ogv",
        ".m4v",
        ".f4v",
        ".f4p",
        ".f4a",
        ".f4b"
    };

    public static HashSet<string> MusicSheetExtensions = new()
    {
        ".mscz",
        ".mscx",
        ".musicxml",
        ".mxl",
        ".md",
        ".cap",
        ".capx",
        ".bww",
        ".mgu",
        ".sgu",
        ".ove",
        ".scw",
        ".ptb"
    };

    public static string ToDisplayString(this FolderContentItem item)
    {
        if (item is FolderItem)
        {
            var type = ((FolderItem)item).Type;
            return type.ToDisplayString();
        }

        if (item is FileItem)
        {
            var extension = ((FileItem)item).Extension;

            return extension switch
            {
                _ when DocumentExtensions.Contains(extension) => "Document",
                _ when AudioExtensions.Contains(extension) => "Audio",
                _ when VideoExtensions.Contains(extension) => "Video",
                _ when ImageExtensions.Contains(extension) => "Image",
                _ when MusicSheetExtensions.Contains(extension) => "Music score",
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

        if (item is FileItem)
        {
            var extension = ((FileItem)item).Extension;

            return extension switch
            {
                _ when DocumentExtensions.Contains(extension) => Icons.Custom.FileFormats.FileDocument,
                _ when AudioExtensions.Contains(extension) => Icons.Custom.FileFormats.FileMusic,
                _ when ImageExtensions.Contains(extension) => Icons.Custom.FileFormats.FileImage,
                _ when VideoExtensions.Contains(extension) => Icons.Custom.FileFormats.FileVideo,
                _ when MusicSheetExtensions.Contains(extension) => Icons.Custom.FileFormats.FileMusic,
                _ => Icons.Material.Filled.InsertDriveFile
            };
        }

        return string.Empty;
    }

    public static string ToIcon(this FolderType type)
    {
        return type switch
        {
            FolderType.CourseRoot => Icons.Material.Filled.Home,
            FolderType.Sessions => Icons.Material.Filled.VideoCameraFront,
            FolderType.Sheets => Icons.Material.Filled.MusicNote,
            FolderType.Recordings => Icons.Material.Filled.VideoCameraFront,
            FolderType.Video => Icons.Material.Filled.VideoLibrary,
            FolderType.Audio => Icons.Material.Filled.LibraryMusic,
            FolderType.Documents => Icons.Material.Filled.LibraryBooks,
            FolderType.Other => Icons.Material.Filled.Workspaces,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    // Return file size string in float format up to 2 decimal places
    public static string ToFileSizeString(this long size)
    {
        if (size < 1024)
        {
            return $"{size} B";
        }

        if (size < 1048576)
        {
            return $"{(float)size / 1024:F2} KB";
        }

        if (size < 1073741824)
        {
            return $"{(float)size / 1048576:F2} MB";
        }

        return $"{(float)size / 1073741824:F2} GB";
    }

    public static string ToDisplayString(this Role role)
    {
        return role switch
        {
            Role.Admin => "Administrator",
            Role.Teacher => "Teacher",
            Role.User => "User",
            Role.Unverified => "Unverified",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}



