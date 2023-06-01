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
using Newtonsoft.Json;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Concerto.Server.Services;
public class DawService
{
    private readonly ILogger<DawService> _logger;
    private readonly IHubContext<DawHub> _dawHubContext;
    private readonly AppDataContext _context;

    public DawService(ILogger<DawService> logger, IHubContext<DawHub> dawHubContext, AppDataContext context)
    {
        _logger = logger;
        _dawHubContext = dawHubContext;
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
            .ToListAsync();
        return project.ToViewModel(userId);
    }

    public async Task<Dto.Track?> GetTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        return track?.ToViewModel(userId);
    }

    public async Task DeleteTrack(long projectId, long trackId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return;
        _context.Tracks.Remove(track);
        await _context.SaveChangesAsync();


        await Task.WhenAll(
            NotifyProjectChanged(projectId),
            track.AudioSourceGuid is not null ? FileExtensions.DeleteAsync(track.AudioSourcePath) : Task.CompletedTask
        );

        await NotifyProjectChanged(projectId);
    }

    public async Task AddTrack(long projectId, string trackName)
    {
        var track = new Track(projectId, trackName);
        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();
        await NotifyProjectChanged(projectId);
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


    public async Task SetTrackStartTime(long projectId, long trackId, float startTime)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return;
        track.StartTime = startTime;
        await _context.SaveChangesAsync();

        await NotifyProjectChanged(track.ProjectId);
    }

    public async Task SetTrackVolume(long projectId, long trackId, float volume)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return;
        track.Volume = volume;
        await _context.SaveChangesAsync();

        await NotifyProjectChanged(track.ProjectId);
    }

    public async Task<bool> SelectTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedBy is not null && track.SelectedBy != userId) return false;
        if (track.SelectedBy == userId) return true;

        track.SelectedBy = userId;
        await _context.SaveChangesAsync();
        await NotifyProjectChanged(track.ProjectId);
        return true;
    }

    public async Task<bool> UnselectTrack(long projectId, long trackId, Guid userId)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null) return false;
        if (track.SelectedBy is null) return true;
        if (track.SelectedBy != userId) return false;

        track.SelectedBy = null;
        await _context.SaveChangesAsync();
        await NotifyProjectChanged(track.ProjectId);
        return true;
    }

    public async Task SetTrackSource(long projectId, long trackId, IFormFile sourceFile)
    {
        var track = await _context.Tracks.FindAsync(projectId, trackId);
        if (track is null)
            throw new Exception($"Track with id {trackId} not found");

        var oldSourceGuid = track.AudioSourceGuid;
        track.AudioSourceGuid = Guid.NewGuid();

        await using (var inputStream = sourceFile.OpenReadStream())
        {
            var inputPipe = new StreamPipeSource(sourceFile.OpenReadStream());
            await FFMpegArguments
                .FromPipeInput(inputPipe)
                .OutputToFile(track.AudioSourcePath, true, opts =>
                    {
                        opts.WithAudioCodec(AudioCodec.LibMp3Lame)
                            .WithCustomArgument("-q:a 0");

                    })
                .ProcessAsynchronously();
        }

        await _context.SaveChangesAsync();

        await Task.WhenAll(
            NotifyProjectChanged(track.ProjectId),
            oldSourceGuid.HasValue ? FileExtensions.DeleteAsync(Track.GetAudioSourcePath(oldSourceGuid.Value)) : Task.CompletedTask
        );

    }

    public async Task GenerateProjectSource(long projectId)
    {
        var project = await _context.DawProjects.FindAsync(projectId);
        if (project is null) return;

        var tracks = await _context.Tracks
            .Where(t => t.ProjectId == projectId)
            .Where(t => t.AudioSourceGuid != null)
            .OrderBy(t => t.Id)
            .ToListAsync();

        if (tracks.Count == 0) return;

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
        if(hashString == project.AudioSourceHash) return;


        var oldSourcePath = project.AudioSourceGuid is null ? null : project.AudioSourcePath;
        project.AudioSourceHash = hashString;
        project.AudioSourceGuid = Guid.NewGuid();

        // Generate combined audio source
        var floatFormat = CultureInfo.InvariantCulture.NumberFormat;
        var adelay = string.Concat(
            tracks.Select((t, i) => $"[{i}]volume={t.Volume.ToString(floatFormat)}[v{i}];[v{i}]adelay={t.StartTimeMilis}|{t.StartTimeMilis}[a{i}];")
        );
        var amix = string.Concat(
            tracks.Select((t, i) => $"[a{i}]")
        );
        amix += $"amix=inputs={tracks.Count}";

        var filterComplex = $"-filter_complex {adelay}{amix}";


        var trackStreams = tracks.Select(t => File.OpenRead(t.AudioSourcePath)).ToList();

        FFMpegArguments ffmpeg = FFMpegArguments.FromPipeInput(new StreamPipeSource(trackStreams.First()));
        foreach (var trackStream in trackStreams.Skip(1))
            ffmpeg = ffmpeg.AddPipeInput(new StreamPipeSource(trackStream));


        await ffmpeg
            .OutputToFile(project.AudioSourcePath, true, opts =>
                {
                    opts.WithCustomArgument(filterComplex)
                        .WithCustomArgument("-q:a 0")
                        .UsingMultithreading(true);
                })
            .ProcessAsynchronously();


        var DisposeTasks = trackStreams.Select(t => t.DisposeAsync()).ToList();
        foreach (var disposeTask in DisposeTasks)
            await disposeTask;

        await _context.SaveChangesAsync();

        if(oldSourcePath is not null)
            await FileExtensions.DeleteAsync(oldSourcePath);
    }

    private async Task NotifyProjectChanged(long projectId)
    {
        await _dawHubContext
            .Clients
            .Group(DawHub.DawProjectGroup(projectId))
            .SendAsync(DawHubMethods.Client.OnProjectChanged, projectId);
    }

}