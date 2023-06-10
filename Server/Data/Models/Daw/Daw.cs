using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Concerto.Server.Settings;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace Concerto.Server.Data.Models;

public class DawProject
{
	[Key, ForeignKey("Session")]
	public long ProjectId { get; set; }
	public virtual ICollection<Track> Tracks {get; set; } = null!;

	public string? AudioSourceHash { get; set; }
	public Guid? AudioSourceGuid { get; set; }

	public string AudioSourcePath
	{
		get
		{
			if(AudioSourceGuid is null) throw new InvalidOperationException($"{nameof(AudioSourceHash)} is null");
			return Track.GetAudioSourcePath(AudioSourceGuid.Value);
		}
	}
}

public class Track
{
	public long Id { get; set; }
	[ForeignKey("Project")]
	public long ProjectId { get; set; }
	public virtual DawProject Project { get; set; } = null!;

	public int Order { get; set; }
	public string Name { get; set; }
	public float StartTime { get; set; }
	public float Volume { get; set; } = 1.0f;

	[ForeignKey("SelectedByUser")]
    public Guid? SelectedByUserId { get; set; }
	public virtual User? SelectedByUser { get; set; }

	public Guid? AudioSourceGuid { get; set; }
	public string AudioSourcePath
	{
		get
		{
			if(AudioSourceGuid is null) throw new InvalidOperationException($"{nameof(AudioSourceGuid)} is null");
			return GetAudioSourcePath(AudioSourceGuid.Value);
		}
	}


	public int StartTimeMilis => (int)(StartTime * 1000);

	public static string GetTempPath()
	{
        return $"{AppSettings.Storage.TempPath}/{Guid.NewGuid()}";
    }

	public static string GetAudioSourcePath(Guid audioSourceGuid)
	{
        return $"{AppSettings.Storage.DawPath}/{audioSourceGuid}.mp3";
    }

	public Track(long projectId, string name)
	{
        Name = name;
        ProjectId = projectId;
    }
}


public static partial class ViewModelConversions
{
	public static Dto.DawProject ToViewModel(this DawProject project, Guid userId)
	{
        return new Dto.DawProject
		{
            Tracks = project.Tracks.Select(t => t.ToViewModel(userId)).ToList()
        };
    }
    public static Dto.Track ToViewModel(this Track track, Guid userId)
	{
        return new Dto.Track
		{
			Id = track.Id,
            Name = track.Name,
			ProjectId = track.ProjectId,
            StartTime = track.StartTime,
			Volume = track.Volume,
			SourceId = track.AudioSourceGuid,
			SelectedByName = track.SelectedByUser?.FullName ?? string.Empty,
            SelectionState = track.SelectedByUserId == null
				? Dto.TrackSelectionState.None
				: track.SelectedByUserId == userId
					? Dto.TrackSelectionState.Self
					: Dto.TrackSelectionState.Other
        };
    }
}