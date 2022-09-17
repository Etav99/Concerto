﻿using Concerto.Shared.Models.Dto;
using System.Net.Http.Json;

namespace Concerto.Client.Contacts;

public class CachedContactsManager : IContactsManager
{
    private readonly HttpClient _http;

    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    public CachedContactsManager(HttpClient http)
    {
        _http = http;
    }


    bool cacheInvalidated = true;
    private List<Dto.User> contactsCache = new List<Dto.User>();
    public List<Dto.User> Contacts
    {
        get
        {
            return contactsCache;
        }
    }

    public async Task LoadContactsAsync()
    {
        await semaphore.WaitAsync();
        if (cacheInvalidated)
        {
            var contactsResponse = await _http.GetFromJsonAsync<Dto.User[]>("User/GetCurrentUserContacts");
            contactsCache = contactsResponse?.ToList() ?? new List<Dto.User>();
            cacheInvalidated = false;
        }
        semaphore.Release();
    }
    
    public void InvalidateCache()
    {
        cacheInvalidated = true;
    }

    public async Task<string> GetContactNameAsync(long contactId)
    {
        await LoadContactsAsync();
        var contact = contactsCache?.FirstOrDefault(c => c.UserId == contactId);
        if (contact != null)
        {
            return $"{contact.FirstName} {contact.LastName}";
        }
        return "Unknown user";
    }
}
