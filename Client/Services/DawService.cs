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

namespace Concerto.Client.Services;

public class DawService : DawClient
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly NavigationManager _navigationManager;
    private readonly HttpClient _httpClient;

    public DawService(HttpClient httpClient, IAccessTokenProvider accessTokenProvider, NavigationManager navigationManager) : base(httpClient)
    {
        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _navigationManager = navigationManager;
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


    public async Task SetTrackSourceAsync(long sessionId, string trackName, IBrowserFile file)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream(maxAllowedSize: 104_857_600)), "file", file.Name);
        content.Add(JsonContent.Create(sessionId, options: JsonSerializerOptions.Default), "sessionId");
        content.Add(new StringContent(trackName), "trackName");

        var response = await _httpClient.PostAsync("/Daw/SetTrackSource", content);
        response.EnsureSuccessStatusCode();
    }

    public static string? GetTrackSourceUrl(Track track)
    {
        if(track.SourceId is null) return null;
        return $"/Daw/GetTrackSource?sessionId={track.SessionId}&trackName={track.Name}";
    }
}