using Nito.AsyncEx;

namespace Concerto.Client.Services;

public interface IRoomService
{
	public IEnumerable<Dto.Room> Rooms { get; }
	public Dictionary<long, IEnumerable<Dto.Session>> RoomSessions { get; }
	public Task LoadRoomsAsync();
	public Task LoadRoomSessions(long roomId);
	public void InvalidateCache();
}
public class RoomService : IRoomService
{
	private readonly IRoomClient _roomClient;
	private readonly ISessionClient _sessionClient;

	private List<Dto.Room> _roomsCache = new();
	private Dictionary<long, IEnumerable<Dto.Session>> _roomSessionsCache = new();
	private bool _cacheInvalidated = true;
	private readonly AsyncLock _mutex = new AsyncLock();
	public RoomService(IRoomClient roomClient, ISessionClient sessionClient)
	{
		_roomClient = roomClient;
		_sessionClient = sessionClient;
	}

	public IEnumerable<Dto.Room> Rooms => _roomsCache;
	public Dictionary<long, IEnumerable<Dto.Session>> RoomSessions => _roomSessionsCache;

	public void InvalidateCache()
	{
		_cacheInvalidated = true;
		_roomSessionsCache.Clear();
	}

	public async Task LoadRoomsAsync()
	{
		using (await _mutex.LockAsync())
		{
			if (!_cacheInvalidated) return;
			var response = await _roomClient.GetCurrentUserRoomsAsync();
			_roomsCache = response?.ToList() ?? new List<Dto.Room>();
			_cacheInvalidated = false;
		}
	}
	public async Task LoadRoomSessions(long roomId)
	{
		using (await _mutex.LockAsync())
		{
			if (_roomSessionsCache.ContainsKey(roomId)) return;
			var response = await _sessionClient.GetRoomSessionsAsync(roomId);
			_roomSessionsCache.Add(roomId, response?.ToList() ?? new List<Dto.Session>());
		}
	}
}
