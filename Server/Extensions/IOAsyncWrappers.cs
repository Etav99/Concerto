namespace Concerto.Server.Extensions;

public static class FileExtensions
{
	public static async Task DeleteAsync(string path)
	{
		await using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous)) { }
	}

	public static async Task DeleteAsync(this FileInfo file)
	{
		await using (new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous)) { }
	}

	public static async Task MoveAsync(string source, string destination, bool createDirectories)
	{
		if (createDirectories)
		{
			var directory = Path.GetDirectoryName(destination);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);
		}

		await using (var tempFileStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 524_288, FileOptions.Asynchronous))
		await using (var fileStream = new FileStream(destination, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 524_288, FileOptions.Asynchronous))
		{
			await tempFileStream.CopyToAsync(fileStream, 524_288);
		}
		await DeleteAsync(source);
	}
}
