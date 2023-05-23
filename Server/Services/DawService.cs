using Concerto.Server.Data.Models.Daw;
using Concerto.Server.Hubs;
using Concerto.Shared.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Services;
public class DawService
{
    private readonly ILogger<DawService> _logger;
	private readonly IHubContext<DawHub> _dawHubContext;

    private Dictionary<long, DawProject> _projects = new();

    public DawService(ILogger<DawService> logger, IHubContext<DawHub> dawHubContext)
    {
        _logger = logger;
        _dawHubContext = dawHubContext;
    }

    public DawProject FindProject(long sessionId)
	{
		if (!_projects.ContainsKey(sessionId))
		{
            _projects[sessionId] = new DawProject();
        }
        return _projects[sessionId];
    }

	public bool CanModifyTrack(Guid userId, long sessionId, string trackName)
	{
        return FindProject(sessionId).Tracks.Any(t => t.Name == trackName && t.SelectedBy == userId);
    }

	public Dto.DawProject GetProject(long sessionId, Guid userId)
	{
		return FindProject(sessionId).ToViewModel(userId);
	}

	public Dto.Track GetTrack(long sessionId, string trackName, Guid userId)
	{
		return FindTrack(sessionId, trackName).ToViewModel(userId);
	}

	public Track FindTrack(long sessionId, string trackName)
	{
        return FindProject(sessionId).Tracks.First(t => t.Name == trackName);
    }

	public async Task DeleteTrack(long sessionId, string trackName)
	{
		var project = FindProject(sessionId);
		var track = project.Tracks.First(t => t.Name == trackName);
        project.Tracks.Remove(track);

		await NotifyProjectChanged(sessionId);
    }

	public async Task AddTrack(long sessionId, string trackName)
	{
        FindProject(sessionId)
			.Tracks
			.Add(new Track { Name = trackName });

		await NotifyProjectChanged(sessionId);
    }

	public async Task SetTrackStartTime(long sessionId, string trackName, float startTime)
	{
		FindTrack(sessionId, trackName)
			.StartTime = startTime;

		await NotifyProjectChanged(sessionId);
	}

	public async Task SetTrackVolume(long sessionId, string trackName, float volume)
	{
		FindTrack(sessionId, trackName)
			.Volume = Math.Clamp(volume, 0, 1);

		await NotifyProjectChanged(sessionId);
	}

	public async Task SetTrackSource(long sessionId, string trackName, byte[] data)
	{
		FindTrack(sessionId, trackName)
			.AudioSource = new DawAudioSource { Guid = Guid.NewGuid(), Bytes = data };

		await NotifyProjectChanged(sessionId);
    }

	public async Task SelectTrack(long sessionId, string trackName, Guid userId)
	{
		FindTrack(sessionId, trackName)
			.SelectedBy = userId;

		await NotifyProjectChanged(sessionId);
	}

	public async Task UnselectTrack(long sessionId, string trackName)
	{
		FindTrack(sessionId, trackName)
            .SelectedBy = null;

		await NotifyProjectChanged(sessionId);
    }

	private async Task NotifyProjectChanged(long sessionId)
	{
        await _dawHubContext
			.Clients
			.Group(DawHub.DawProjectGroup(sessionId))
			.SendAsync(DawHubMethods.Client.ProjectChanged, sessionId);
    }


}