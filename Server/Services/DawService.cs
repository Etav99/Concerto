using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Hubs;
using Concerto.Server.Settings;
using Concerto.Shared.Constants;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MimeMapping;
using Nito.AsyncEx;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Concerto.Server.Services;
public class DawService
{
    private readonly ILogger<DawService> _logger;
    private readonly ConcertoDbContext _context;

    private static AsyncLock ProjectGenerationLock = new();
    public static Dictionary<long, SemaphoreSlim> ProjectGenerationAsyncLocks { get; } =  new();

	public DawService(ILogger<DawService> logger, ConcertoDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	public async Task<Dto.DawProject> GetProject(long projectId, Guid userId)
    {
        var project = await _context.DawProjects.FindAsync(projectId);
        if (project == null)
        {
            project = new DawProject { ProjectId = projectId };
            _context.DawProjects.Add(project);
            await _context.SaveChangesAsync();
        }

        project.Tracks = await _context.Tracks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Id)
            .Include(t => t.SelectedByUser)
            .ToListAsync();

        return project.ToViewModel(userId);
    }

    public async Task<Dto.Track?> GetTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track == null) return null;

        if(track.SelectedByUserId is not null)
            await _context.Entry(track).Reference(t => t.SelectedByUser).LoadAsync();

        return track.ToViewModel(userId);
    }

    public async Task<bool> DeleteTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId != userId) return false;

        _context.Tracks.Remove(track);
        await _context.SaveChangesAsync();
        await (track.AudioSourceGuid is not null ? FileExtensions.DeleteAsync(track.AudioSourcePath) : Task.CompletedTask);
        return true;
    }

    public async Task<bool> AddTrack(long projectId, string trackName)
    {
        var track = new Track(projectId, trackName);
        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FileStream> GetTrackSourceStream(long projectId, long trackId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) throw new Exception($"Track with id {trackId} not found");

        return new FileStream(track.AudioSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, AppSettings.Storage.StreamBufferSize, FileOptions.Asynchronous);
    }

    public async Task<(FileStream, string)> GetProjectSourceStream(long projectId)
    {
        var project = await _context.DawProjects.FindAsync(projectId);
        if (project is null) throw new Exception($"Project with id {projectId} not found");
        if (project.AudioSourceHash is null || project.AudioSourceGuid is null) throw new Exception($"Project with id {projectId} has no audio source");

        return (new FileStream(project.AudioSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, AppSettings.Storage.StreamBufferSize, FileOptions.Asynchronous), project.AudioSourceHash);
    }

    public async Task<bool> SetTrackName(long projectId, long trackId, string trackName, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if(track.SelectedByUserId != userId) return false;

        track.Name = trackName;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetTrackStartTime(long projectId, long trackId, float startTime, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId != userId) return false;

        if (track.StartTime == startTime) return false;

        track.StartTime = startTime;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetTrackVolume(long projectId, long trackId, float volume, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId != userId) return false;
        if(track.Volume == volume) return false;

        track.Volume = volume;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SelectTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId is not null) return false;

        track.SelectedByUserId = userId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnselectTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId != userId) return false;

        track.SelectedByUserId = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetTrackSource(long projectId, long trackId, IFormFile sourceFile, Guid userId, float? startTime = null, float? volume = null)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedByUserId != userId) return false;

        var oldSourceGuid = track.AudioSourceGuid;
        track.AudioSourceGuid = Guid.NewGuid();

        await using (var inputStream = sourceFile.OpenReadStream())
        {
            var inputPipe = new StreamPipeSource(inputStream);
            await FFMpegArguments
                .FromPipeInput(inputPipe)
                .OutputToFile(track.AudioSourcePath, true, opts =>
                    {
                        opts.WithAudioCodec(AudioCodec.LibMp3Lame)
                            .WithCustomArgument("-q:a 0");

                    })
                .ProcessAsynchronously();
        }

        if(startTime.HasValue)
            track.StartTime = startTime.Value;
        if(volume.HasValue)
            track.Volume = Math.Clamp(volume.Value, 0, 1);

        await _context.SaveChangesAsync();
        await (oldSourceGuid.HasValue ? FileExtensions.DeleteAsync(Track.GetAudioSourcePath(oldSourceGuid.Value)) : Task.CompletedTask);
        return true;
    }


    public async Task SaveProjectSource(long projectId, long folderId, string filename, Guid userId)
    {
        var project = await _context.DawProjects.FindAsync(projectId);
        if(project is null) throw new Exception($"Project with id {projectId} not found");

        var mutex = await GetProjectGenerationLock(projectId);
        await mutex.WaitAsync();

        if(!File.Exists(project?.AudioSourcePath)) throw new Exception($"Project with id {projectId} has no audio source file");

        var sanitizedFilename = WebUtility.HtmlEncode(filename);
		if (string.IsNullOrEmpty(sanitizedFilename)) throw new FilenameException("Filename is empty");

        var uploadedFile = new UploadedFile
        {
                OwnerId = userId,
				DisplayName = sanitizedFilename,
				Extension = ".mp3",
                MimeType = MimeUtility.GetMimeMapping(".mp3"),
				Size = new FileInfo(project.AudioSourcePath).Length,
				StorageName = string.Format(@$"{sanitizedFilename}.mp3.{Guid.NewGuid()}"),
				FolderId = folderId
	    };

        await _context.UploadedFiles.AddAsync(uploadedFile);
        await FileExtensions.CopyAsync(project.AudioSourcePath, uploadedFile.Path, true);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            await FileExtensions.DeleteAsync(uploadedFile.Path);
            throw;
        }
        finally
        {
            mutex.Release();
            await ReturnProjectGenerationLock(projectId);
        }
    }

    public async Task<bool> GenerateProjectSource(long projectId)
    {
        var mutex = await GetProjectGenerationLock(projectId);
        await mutex.WaitAsync();
        try
        {
            return await GenerateProjectSourceInternal(projectId);
        }
        finally
        {
            mutex.Release();
            await ReturnProjectGenerationLock(projectId);
        }
    }



    public async Task<SemaphoreSlim> GetProjectGenerationLock(long projectId)
    {
        using(await ProjectGenerationLock.LockAsync())
        {
            ProjectGenerationAsyncLocks.TryGetValue(projectId, out var projectLock);
            if (projectLock is not null)
            {
                return projectLock;
            }
            else
            {
                var newLock = new SemaphoreSlim(1, 1);
                ProjectGenerationAsyncLocks.Add(projectId, newLock);
                return newLock;
            }
        }
    }

    public async Task ReturnProjectGenerationLock(long projectId)
    {
        using(await ProjectGenerationLock.LockAsync())
        {
            if (!ProjectGenerationAsyncLocks.TryGetValue(projectId, out var projectLock))
                return;

            if (projectLock.CurrentCount == 0)
                return;

            ProjectGenerationAsyncLocks.Remove(projectId);
        }
    }

    public async Task<bool> GenerateProjectSourceInternal(long projectId)
    {
        var project = await _context.DawProjects.FindAsync(projectId);
        if (project is null) return false;

        var tracks = await _context.Tracks
            .Where(t => t.ProjectId == projectId)
            .Where(t => t.AudioSourceGuid != null)
            .OrderBy(t => t.Id)
            .ToListAsync();

        if (tracks.Count == 0) return false;

        // Calculate MD5 hash of all tracks state
        string hashString;
        using (var mD5 = MD5.Create())
        {
            var toHash = string.Concat(tracks.Select(t => t.AudioSourceGuid))
                + string.Concat(tracks.Select(t => t.StartTime))
                + string.Concat(tracks.Select(t => t.Volume));

            var hash = mD5.ComputeHash(Encoding.UTF8.GetBytes(toHash));
            hashString = Convert.ToHexString(hash);
        }
        if(hashString == project.AudioSourceHash) return false;


        var oldSourcePath = project.AudioSourceGuid is null ? null : project.AudioSourcePath;
        project.AudioSourceHash = hashString;
        project.AudioSourceGuid = Guid.NewGuid();

        // Generate combined audio source
        var numberFormat = CultureInfo.InvariantCulture.NumberFormat;
        var adelay = string.Concat(
            tracks.Select((t, i) =>
            {
                // trim if negative start time
                if (t.StartTime < 0)
                {
                    double trimStart = Math.Abs(t.StartTime);
                    return $"[{i}]atrim=start={trimStart.ToString(numberFormat)}[t{i}];[t{i}]volume={t.Volume.ToString(numberFormat)}[a{i}];";
                }
                // delay if positive start time
                return $"[{i}]volume={t.Volume.ToString(numberFormat)}[v{i}];[v{i}]adelay=delays={t.StartTimeMilis}:all=1[a{i}];";
            })
        );
        var amix = string.Concat(
            tracks.Select((t, i) => $"[a{i}]")
        );
        amix += $"amix=inputs={tracks.Count}";

        var filterComplex = $"-filter_complex {adelay}{amix}";


        var trackStreams = tracks.Select(t => (t.AudioSourcePath)).ToList();

        FFMpegArguments ffmpeg = FFMpegArguments.FromFileInput(trackStreams.First());
        foreach (var trackStream in trackStreams.Skip(1))
            ffmpeg = ffmpeg.AddFileInput(trackStream);


        await ffmpeg
            .OutputToFile(project.AudioSourcePath, true, opts =>
                {
                    opts.WithCustomArgument(filterComplex)
                        .WithCustomArgument("-q:a 0")
                        .UsingMultithreading(true);
                })
            .ProcessAsynchronously();


        //var DisposeTasks = trackStreams.Select(t => t.DisposeAsync()).ToList();
        //foreach (var disposeTask in DisposeTasks)
        //    await disposeTask;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            await FileExtensions.DeleteAsync(project.AudioSourcePath);
			throw;
        }
        finally
        {
            if(oldSourcePath is not null)
                await FileExtensions.DeleteAsync(oldSourcePath);
        }

    }




}