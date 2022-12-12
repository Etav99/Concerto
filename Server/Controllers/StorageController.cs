using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
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
    private readonly StorageService _storageService;
    private readonly SessionService _sessionService;
	private long UserId => HttpContext.UserId();

	public StorageController(ILogger<StorageController> logger, StorageService storageService, SessionService sessionService)
    {
        _logger = logger;
        _storageService = storageService;
        _sessionService = sessionService;
	}


    [HttpGet]
    public async Task<ActionResult<Dto.FolderContent>> GetFolderContent([FromQuery] long folderId)
    {
        if (User.IsAdmin())
        {
            return Ok(await _storageService.GetFolderContent(folderId, UserId, true));
        }
        
        if (!await _storageService.CanReadInFolder(UserId, folderId)) return Forbid();
        return Ok(await _storageService.GetFolderContent(folderId, UserId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.FolderSettings>> GetFolderSettings([FromQuery] long folderId)
    {
        if (User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, folderId))
        {
            return Ok(await _storageService.GetFolderSettings(folderId));
        }
        return Forbid();
    }

    [HttpGet]
    public async Task<ActionResult<Dto.FileSettings>> GetFileSettings([FromQuery] long fileId)
    {
        if (User.IsAdmin() || await _storageService.CanManageFile(UserId, fileId))
        {
            return Ok(await _storageService.GetFileSettings(fileId));
        }
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
    public async Task<ActionResult> CreateFolder([FromBody] Dto.CreateFolderRequest createFolderRequest)
    {
        if(User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, createFolderRequest.ParentId))
        {
            await _storageService.CreateFolder(createFolderRequest, UserId);
            return Ok();
        }
        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult> UpdateFolder([FromBody] Dto.UpdateFolderRequest updateFolderRequest)
    {
        if (User.IsAdmin() || await _storageService.CanEditFolder(UserId, updateFolderRequest.Id))
        {
            await _storageService.UpdateFolder(updateFolderRequest);
            return Ok();
        }
        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long folderId)
    {
        if (User.IsAdmin() || await _storageService.CanWriteInFolder(UserId, folderId))
        {
            var fileUploadResults = await _storageService.AddFilesToFolder(files, folderId);
            return Ok(fileUploadResults);
        }
        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult> UpdateFile(Dto.UpdateFileRequest request)
    {
        if (User.IsAdmin() || await _storageService.CanManageFile(UserId, request.FileId))
        {
            if(await _storageService.UpdateFile(request))
            return Ok();
        }
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
	public async Task<ActionResult> DeleteFolderItems([FromBody] Dto.DeleteFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

        if (!User.IsAdmin())
        {
			foreach (var folderId in folderIds)
			{
                if (!await _storageService.CanDeleteFolder(UserId, folderId)) return Forbid();
		    }
			
			foreach (var fileId in fileIds)
			{
				if (!await _storageService.CanManageFile(UserId, fileId)) return Forbid();
			}
		}

		await _storageService.DeleteFolders(folderIds);
		await _storageService.DeleteFiles(fileIds);
        return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> MoveFolderItems([FromBody] Dto.MoveFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

		if (!User.IsAdmin())
		{
			if (!await _storageService.CanWriteInFolder(UserId, request.DestinationFolderId)) return Forbid();

			foreach (var folderId in folderIds)
			{
				if (!await _storageService.CanMoveFolder(UserId, folderId)) return Forbid();
			}

			foreach (var fileId in fileIds)
			{
				if (!await _storageService.CanManageFile(UserId, fileId)) return Forbid();
			}
		}

		await _storageService.MoveFolders(folderIds, request.DestinationFolderId);
		await _storageService.MoveFiles(fileIds, request.DestinationFolderId);
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult> CopyFolderItems([FromBody] Dto.CopyFolderItemsRequest request)
	{
		var folderIds = request.FolderIds.ToList();
		var fileIds = request.FileIds.ToList();

		if (!User.IsAdmin())
		{
			if (!await _storageService.CanWriteInFolder(UserId, request.DestinationFolderId)) return Forbid();

			foreach (var folderId in folderIds)
			{
				if (!await _storageService.CanReadInFolder(UserId, folderId)) return Forbid();
			}

			foreach (var fileId in fileIds)
			{
				if (!await _storageService.CanReadFile(UserId, fileId)) return Forbid();
			}
		}

		await _storageService.CopyFolders(folderIds, request.DestinationFolderId, UserId);
		await _storageService.CopyFiles(fileIds, request.DestinationFolderId, UserId);
		return Ok();
	}


	[HttpGet]
    public async Task<ActionResult> DownloadFile([FromQuery] long fileId)
    {
        if (User.IsAdmin() || await _storageService.CanReadFile(UserId, fileId))
        {
            var file = await _storageService.GetFile(fileId);
            if (file == null) return NotFound();
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
            string fileName = file.DisplayName + file.Extension;
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        return Forbid();
    }

}
