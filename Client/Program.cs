global using Dto = Concerto.Shared.Models.Dto;
using Concerto.Client;
using Concerto.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using static System.Net.WebRequestMethods;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var http = new HttpClient()
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
};
using var response = await http.GetAsync("appsettings.json");
using var stream = await response.Content.ReadAsStreamAsync();
builder.Configuration.AddJsonStream(stream);

builder.Services.AddHttpClient("WebAPI",
        client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("WebAPI"));

builder.Services.AddScoped<IChatManager, CachedChatManager>();
builder.Services.AddScoped<IContactManager, CachedContactManager>();
builder.Services.AddMudServices();

builder.Services.AddOidcAuthentication(options =>
{
        options.ProviderOptions.Authority = builder.Configuration["authorityUrl"];
        options.ProviderOptions.ClientId = "concerto-client";
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.PostLogoutRedirectUri = builder.Configuration["redirectUrl"];
        options.ProviderOptions.DefaultScopes.Add("roles");
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger<Program>();

logger.LogInformation($"Remote = {Environment.GetEnvironmentVariable("ASPNETCORE_REMOTE")?.Equals("true") ?? false}");

await host.RunAsync();