using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using static MudBlazor.CategoryTypes;
using System.Net.Http.Json;
using System.Text.Json;
using System;
using static System.Net.WebRequestMethods;
using System.Net.Http;
using Microsoft.JSInterop;

namespace Concerto.Client.Services;

public class DawService : DawClient
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly NavigationManager _navigationManager;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;

    public DawService(HttpClient httpClient, IAccessTokenProvider accessTokenProvider, NavigationManager navigationManager, IJSRuntime js) : base(httpClient)
    {
        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _navigationManager = navigationManager;
        _js = js;
    }

    public HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("daw"), options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var accessTokenResult = await _accessTokenProvider.RequestAccessToken();
                        accessTokenResult.TryGetToken(out var accessToken);
                        return accessToken.Value;
                    };
                }
        )
        .Build();
    }


    public async Task SetTrackSourceAsync(long sessionId, long trackId, IBrowserFile file)
    {
        await SetTrackSourceAsync(sessionId, trackId, file.OpenReadStream(maxAllowedSize: 104_857_600));
    }

    public async Task SetTrackSourceAsync(long sessionId, long trackId, string url)
    {
        // create stream from url
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await SetTrackSourceAsync(sessionId, trackId, await response.Content.ReadAsStreamAsync());

        if(url.StartsWith("blob:"))
            await _js.InvokeVoidAsync("URL.revokeObjectURL", url);
    }

    private async Task SetTrackSourceAsync(long sessionId, long trackId, Stream stream)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", "file");
        content.Add(JsonContent.Create(sessionId, options: JsonSerializerOptions.Default), "projectId");
        content.Add(JsonContent.Create(trackId, options: JsonSerializerOptions.Default), "trackId");

        var response = await _httpClient.PostAsync("/Daw/SetTrackSource", content);
        response.EnsureSuccessStatusCode();
        
        await stream.DisposeAsync();
    }

    public static string GetProjectSourceUrl(long projectId)
    {
        return $"/Daw/GetProjectSource?projectId={projectId}&guid={Guid.NewGuid()}";
    }

    public static string? GetTrackSourceUrl(Track track)
    {
        if(track.SourceId is null) return null;
        return $"/Daw/GetTrackSource?projectId={track.ProjectId}&trackId={track.Id}";
    }
}