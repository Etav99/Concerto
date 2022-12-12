using Concerto.Server.Data.Models;

namespace Concerto.Server.Extensions;
public static class ViewModelConversions
{
	public static Dto.User ToViewModel(this User user)
	{
		return new Dto.User
		{
			Id = user.Id,
			Username = user.Username,
			FirstName = user.FirstName,
			LastName = user.LastName
		};
	}

	public static IEnumerable<Dto.User> ToViewModel(this IEnumerable<User>? users)
	{
		if (users == null)
			return Enumerable.Empty<Dto.User>();
		return users.Select(c => c.ToViewModel());
	}

	public static Dto.UploadedFile ToViewModel(this UploadedFile file)
	{
		return new Dto.UploadedFile(
			Id: file.Id,
			Name: file.DisplayName
		);
	}
	public static IEnumerable<Dto.UploadedFile> ToViewModel(this IEnumerable<UploadedFile> files)
	{
		return files.Select(u => u.ToViewModel());
	}

	public static Dto.FileUploadResult ToViewModel(this FileUploadResult fileUploadResult)
	{
		return new Dto.FileUploadResult
		{
			DisplayFileName = fileUploadResult.DisplayFileName,
			ErrorCode = fileUploadResult.ErrorCode,
			Uploaded = fileUploadResult.Uploaded
		};
	}

	public static IEnumerable<Dto.FileUploadResult> ToViewModel(this IEnumerable<FileUploadResult> fileUploadResults)
	{
		return fileUploadResults.Select(u => u.ToViewModel());
	}


}
