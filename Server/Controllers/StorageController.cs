using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Server.Settings;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Mime;
using System.Text.Json;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class StorageController : ControllerBase
{
	private readonly ILogger<StorageController> _logger;
	private readonly StorageService _storageService;

	private Guid UserId => HttpContext.UserId();

	public StorageController(ILogger<StorageController> logger, StorageService storageService, SessionService sessionService)
	{
		_logger = logger;
		_storageService = storageService;
	}

	[HttpGet]
	public async Task<ActionResult<FolderContent>> GetFolderContent([FromQuery] long folderId)
	{
		if (User.IsAdmin()) return Ok(await _storageService.GetFolderContent(folderId, UserId, true));

		if (!await _storageService.CanReadInFolder(UserId, folderId)) return Forbid();
		return Ok(await _storageService.GetFolderContent(folderId, UserId));
	}

	[HttpGet]
	public async Task<ActionResult<FolderSettings>> GetFolderSettings([FromQuery] long folderId)
	{
		if (User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, folderId))
			return Ok(await _storageService.GetFolderSettings(folderId));
		return Forbid();
	}

	[HttpGet]
	public async Task<ActionResult<FileSettings>> GetFileSettings([FromQuery] long fileId)
	{
		if (User.IsAdmin() || await _storageService.CanManageFile(UserId, fileId)) return Ok(await _storageService.GetFileSettings(fileId));
		return Forbid();
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteFolder([FromQuery] long folderId)
	{
		if (User.IsAdmin() || await _storageService.CanDeleteFolder(UserId, folderId))
		{
			await _storageService.DeleteFolder(folderId);
			return Ok();
		}

		return Forbid();
	}

	[HttpPost]
	public async Task<ActionResult<long>> CreateFolder([FromBody] CreateFolderRequest createFolderRequest)
	{
		if (!User.IsAdmin() && !await _storageService.CanWriteInFolder(UserId, createFolderRequest.ParentId)) return Forbid();


		var folderId = await _storageService.CreateFolder(createFolderRequest, UserId);
		if (folderId == null) return BadRequest();
		return Ok(folderId);
	}

	[HttpPost]
	public async Task<ActionResult> UpdateFolder([FromBody] UpdateFolderRequest updateFolderRequest)
	{
		if (!User.IsAdmin() && !await _storageService.CanEditFolder(UserId, updateFolderRequest.Id)) return Forbid();

		await _storageService.UpdateFolder(updateFolderRequest);
		return Ok();
	}

	[HttpPost]
	[RequestFormLimits(MemoryBufferThreshold = 1024 * 1024 * 1024)]
	public async Task<ActionResult<FileUploadResult?>> UploadFileChunk([FromForm] IFormFile file, [FromForm] string chunk)
	{
		var chunkMetadata = JsonSerializer.Deserialize<FileChunkMetadata>(chunk);
		if (chunkMetadata is null) return BadRequest();
		if (StorageService.IsFirstChunk(chunkMetadata) && !User.IsAdmin() && !await _storageService.CanWriteInFolder(UserId, chunkMetadata.FolderId))
			return Forbid();

		bool lastChunk = await _storageService.SaveChunk(chunkMetadata, file);
		if (lastChunk)
		{
			if (User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, chunkMetadata.FolderId))
				return Ok(await _storageService.SaveUploadedFile(chunkMetadata, file.FileName, UserId));

			await _storageService.AbortFileUpload(chunkMetadata);
			return Forbid();
		}
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> AbortFileUpload(FileChunkMetadata chunkMetadata)
	{
		await _storageService.AbortFileUpload(chunkMetadata);
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> UpdateFile(UpdateFileRequest request)
	{
		if (!User.IsAdmin() && !await _storageService.CanManageFile(UserId, request.FileId))
			return Forbid();

		if (await _storageService.UpdateFile(request))
			return Ok();

		return Forbid();
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteFile([FromQuery] long fileId)
	{
		if (!User.IsAdmin() && !await _storageService.CanManageFile(UserId, fileId))
			return Forbid();

		await _storageService.DeleteFile(fileId);
		return Ok();
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteFolderItems([FromBody] DeleteFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

		if (!User.IsAdmin())
		{
			foreach (var folderId in folderIds)
				if (!await _storageService.CanDeleteFolder(UserId, folderId))
					return Forbid();

			foreach (var fileId in fileIds)
				if (!await _storageService.CanManageFile(UserId, fileId))
					return Forbid();
		}

		await _storageService.DeleteFolders(folderIds);
		await _storageService.DeleteFiles(fileIds);
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> MoveFolderItems([FromBody] MoveFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

		if (!User.IsAdmin())
		{
			if (!await _storageService.CanWriteInFolder(UserId, request.DestinationFolderId)) return Forbid();

			foreach (var folderId in folderIds)
				if (!await _storageService.CanMoveFolder(UserId, folderId))
					return Forbid();

			foreach (var fileId in fileIds)
				if (!await _storageService.CanManageFile(UserId, fileId))
					return Forbid();

			foreach (var folderId in folderIds)
				if (!await _storageService.CanFolderBeMoved(folderId, request.DestinationFolderId))
					return BadRequest();

			foreach (var fileId in fileIds)
				if (!await _storageService.CanFileBeMoved(fileId, request.DestinationFolderId))
					return BadRequest();
		}

		await _storageService.MoveFolders(folderIds, request.DestinationFolderId);
		await _storageService.MoveFiles(fileIds, request.DestinationFolderId);
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> CopyFolderItems([FromBody] CopyFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

		if (!User.IsAdmin())
		{
			if (!await _storageService.CanWriteInFolder(UserId, request.DestinationFolderId)) return Forbid();

			foreach (var folderId in folderIds)
				if (!await _storageService.CanReadInFolder(UserId, folderId))
					return Forbid();

			foreach (var fileId in fileIds)
				if (!await _storageService.CanReadFile(UserId, fileId))
					return Forbid();
		}

		await _storageService.CopyFolders(folderIds, request.DestinationFolderId, UserId);
		await _storageService.CopyFiles(fileIds, request.DestinationFolderId, UserId);
		return Ok();
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult> DownloadFile([FromQuery] long fileId, [FromQuery] Guid token, [FromQuery] bool inline)
	{
		if (!_storageService.ValidateToken(token, fileId, StorageService.TokenType.File)) return Forbid();

		var file = await _storageService.GetFile(fileId);
		if (file == null) return NotFound();
		var fileBytes = System.IO.File.OpenRead(file.Path);

		if(inline)
			return File(fileStream: fileBytes, contentType: file.MimeType, enableRangeProcessing: true);

		return File(fileStream: fileBytes, fileDownloadName: file.DisplayName + file.Extension, contentType: file.MimeType, enableRangeProcessing: true);
	}

	[HttpGet]
	public async Task<ActionResult<Guid>> GetFileDownloadToken([FromQuery] long fileId)
	{
		if (!(User.IsAdmin() || await _storageService.CanReadFile(UserId, fileId))) return Forbid();

		var token = _storageService.GenerateToken(fileId, StorageService.TokenType.File);
		return Ok(token);
	}
}

