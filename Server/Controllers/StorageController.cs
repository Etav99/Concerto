﻿using System.Net.Mime;
using System.Text.Json;
using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class StorageController : ControllerBase
{
	private readonly ILogger<StorageController> _logger;
	private readonly SessionService _sessionService;
	private readonly StorageService _storageService;

	public StorageController(ILogger<StorageController> logger, StorageService storageService, SessionService sessionService)
	{
		_logger = logger;
		_storageService = storageService;
		_sessionService = sessionService;
	}

	private long UserId => HttpContext.UserId();


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
	public async Task<ActionResult> CreateFolder([FromBody] CreateFolderRequest createFolderRequest)
	{
		if (User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, createFolderRequest.ParentId))
		{
			await _storageService.CreateFolder(createFolderRequest, UserId);
			return Ok();
		}

		return Forbid();
	}

	[HttpPost]
	public async Task<ActionResult> UpdateFolder([FromBody] UpdateFolderRequest updateFolderRequest)
	{
		if (User.IsAdmin() || await _storageService.CanEditFolder(UserId, updateFolderRequest.Id))
		{
			await _storageService.UpdateFolder(updateFolderRequest);
			return Ok();
		}

		return Forbid();
	}
	
	
	[HttpPost]
	[RequestFormLimits(MemoryBufferThreshold = 1024 * 1024 * 1024)]
	public async Task<ActionResult<FileUploadResult?>> UploadFileChunk([FromForm] IFormFile file, [FromForm] string chunk)
	{
		var chunkMetadata = JsonSerializer.Deserialize<FileChunkMetadata>(chunk);
		if (chunkMetadata is null) return BadRequest();
		if (_storageService.IsFirstChunk(chunkMetadata) && !(User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, chunkMetadata.FolderId)))
			return Forbid();
		
		if(await _storageService.SaveChunk(chunkMetadata, file))
		{
			if (!User.IsAdmin() && !await _storageService.CanWriteInFolder(UserId, chunkMetadata.FolderId))
			{
				_storageService.AbortFileUpload(chunkMetadata);
				return Forbid();
			}
			return Ok(await _storageService.SaveUploadedFile(chunkMetadata, file.FileName, UserId));
		}
		return Ok();
	}

	[HttpPost]
	public ActionResult AbortFileUpload(FileChunkMetadata chunkMetadata)
	{
		_storageService.AbortFileUpload(chunkMetadata);
		return Ok();
	}


	[HttpPost]
	public async Task<ActionResult> UpdateFile(UpdateFileRequest request)
	{
		if (User.IsAdmin() || await _storageService.CanManageFile(UserId, request.FileId))
			if (await _storageService.UpdateFile(request))
				return Ok();
		return Forbid();
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteFile([FromQuery] long fileId)
	{
		if (User.IsAdmin() || await _storageService.CanManageFile(UserId, fileId))
		{
			await _storageService.DeleteFile(fileId);
			return Ok();
		}

		return Forbid();
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
	public async Task<ActionResult> DownloadFile([FromQuery] long fileId, [FromQuery] Guid token)
	{
		if (!_storageService.ValidateToken(fileId, token)) return Forbid();
		
		var file = await _storageService.GetFile(fileId);
		if (file == null) return NotFound();
		var fileBytes = System.IO.File.OpenRead(file.Path);
		var fileName = file.DisplayName + file.Extension;
		return File(fileBytes, MediaTypeNames.Application.Octet, fileName);
	}

	[HttpGet]
	public async Task<ActionResult<Guid>> GetOneTimeToken([FromQuery] long fileId)
	{
		if (!(User.IsAdmin() || await _storageService.CanReadFile(UserId, fileId))) return Forbid();

		var token = _storageService.GenerateOneTimeToken(fileId);
		return Ok(token);
	}
}

