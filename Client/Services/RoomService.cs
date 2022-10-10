﻿using Concerto.Shared.Models.Dto;
using Nito.AsyncEx;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

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
    private readonly HttpClient _http;

    private List<Dto.Room> _roomsCache = new();
    private Dictionary<long, IEnumerable<Dto.Session>> _roomSessionsCache = new();
    private bool _cacheInvalidated = true;
    private readonly AsyncLock _mutex = new AsyncLock();

    public RoomService(HttpClient http)
    {
        _http = http;
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
            var response = await _http.GetFromJsonAsync<Dto.Room[]>("Room/GetCurrentUserRooms");
            _roomsCache = response?.ToList() ?? new List<Dto.Room>();
            _cacheInvalidated = false;
        }
    }
    public async Task LoadRoomSessions(long roomId)
    {
        using (await _mutex.LockAsync())
        {
            if (_roomSessionsCache.ContainsKey(roomId)) return;
            var response = await _http.GetFromJsonAsync<Dto.Session[]>($"Session/GetRoomSessions?roomId={roomId}");
            _roomSessionsCache.Add(roomId, response?.ToList() ?? new List<Dto.Session>());
        }
    }
}
