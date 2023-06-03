global using Dto = Concerto.Shared.Models.Dto;
using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Extensions;
using Concerto.Server.Hubs;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Server.Settings;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Diagnostics;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Concerto.Server Builder");
// Add services to the container.
// IdentityModelEventSource.ShowPII = true; 

builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddHostedService<ScheduledTasksService>();
builder.Services.AddSingleton<OneTimeTokenStore, OneTimeTokenStore>();
builder.Services.AddSingleton<DawProjectStateService, DawProjectStateService>();
builder.Services.AddScoped<DawService, DawService>();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ForumService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<IdentityManagerService>();

// builder.Services.AddResponseCompression(opts =>
// {
//     opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//         new[] { "application/octet-stream" });
// });

builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		}
	)
	.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.RequireHttpsMetadata = AppSettings.Oidc.RequireHttpsMetadata;

			if (AppSettings.Oidc.AcceptAnyServerCertificateValidator)
				options.BackchannelHttpHandler = new HttpClientHandler
				{
					ServerCertificateCustomValidationCallback =
						HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
				};

			options.MetadataAddress = AppSettings.Oidc.MetadataAddress;
			options.Authority = AppSettings.Oidc.Authority;
			options.Audience = AppSettings.Oidc.Audience;

			options.Events = new JwtBearerEvents
			{
				OnMessageReceived = context =>
				{
					if (string.IsNullOrEmpty(context.Token))
					{
						var accessToken = context.Request.Query["access_token"];
						if (!string.IsNullOrEmpty(accessToken))
						{
							context.Token = accessToken;
						}
					}
					return Task.CompletedTask;
				}
			};
		}
	)
	.AddCookie(options =>
	{
		options.Cookie.Path = $"{AppSettings.Web.BasePath}/auth";
		options.Cookie.SameSite = SameSiteMode.Strict;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
	})
	.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
		options.RequireHttpsMetadata = AppSettings.Oidc.RequireHttpsMetadata;
		options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
		options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

		options.Authority = AppSettings.Oidc.Authority;
		options.ClientId = AppSettings.Oidc.ServerClientId;
		options.ClientSecret = AppSettings.Oidc.ServerClientSecret;
		options.ResponseType = "code";
		options.Scope.Add("openid");

		options.SaveTokens = true;

        options.CallbackPath = "/auth/signin-oidc";
        options.SignedOutCallbackPath = "/auth/signout-callback-oidc";

		options.Events = new OpenIdConnectEvents
		{
			OnRedirectToIdentityProvider = context =>
			{
				var builder = new UriBuilder(context.ProtocolMessage.RedirectUri);
                builder.Scheme = "https";
                builder.Port = -1;
                context.ProtocolMessage.RedirectUri = builder.ToString();
				return Task.CompletedTask;
			}
		};
	});


builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.IsAuthenticated.Name, AuthorizationPolicies.IsAuthenticated.Policy());
	options.AddPolicy(AuthorizationPolicies.IsVerified.Name, AuthorizationPolicies.IsVerified.Policy());
	options.AddPolicy(AuthorizationPolicies.IsNotVerified.Name, AuthorizationPolicies.IsNotVerified.Policy());
	options.AddPolicy(AuthorizationPolicies.IsAdmin.Name, AuthorizationPolicies.IsAdmin.Policy());
	options.AddPolicy(AuthorizationPolicies.IsTeacher.Name, AuthorizationPolicies.IsTeacher.Policy());
	options.DefaultPolicy = AuthorizationPolicies.IsVerified.Policy();
});


// Configure database context
builder.Services.AddDbContext<AppDataContext>(options =>
	options.UseNpgsql(AppSettings.Database.DbString)
);
builder.Services.AddScoped<AppDataContext>();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
	// Allow any origin
	builder.Services.AddCors(options =>
	{
		options.AddPolicy("DevPolicy", builder =>
		 builder.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader());
	});
}

var app = builder.Build();

app.UsePathBase($"/{AppSettings.Web.BasePath.Trim('/')}");

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor API V1"); });


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
	app.UseCors("DevPolicy");
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}


// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseUserIdMapperMiddleware();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<NotificationHub>("/notifications");
app.MapHub<DawHub>("/daw");

var contentRoot = app.Environment.WebRootFileProvider;
// load index.html
var indexHtmlTemplate = contentRoot.GetFileInfo("index_template.html");

var indexHtmlContent = File.ReadAllText(indexHtmlTemplate.PhysicalPath!);
var basePath = AppSettings.Web.BasePath.Trim('/');
basePath = basePath.IsNullOrEmpty() ? basePath : basePath + "/";
var baseTag = @$"<base href=""/{basePath}"" />";

// replace base tag with <base href="/{basePath}/"> 
var indexHtmlContentWithBase = Regex.Replace(indexHtmlContent, "<base *href=\".*?\".*?>", baseTag);
// write to index_base.html
var indexHtml = Path.Combine(Path.GetDirectoryName(indexHtmlTemplate.PhysicalPath)!, "index.html");
File.WriteAllText(indexHtml, indexHtmlContentWithBase);

app.MapFallbackToFile("index.html");


var diagnosticSource = app.Services.GetRequiredService<DiagnosticListener>();
using var badRequestListener = new BadRequestEventListener(diagnosticSource, (badRequestExceptionFeature) =>
{
	app.Logger.LogError(badRequestExceptionFeature.Error, "Bad request received");
});

await using var scope = app.Services.CreateAsyncScope();
await using (var db = scope.ServiceProvider.GetService<AppDataContext>())
{
	if (db == null) throw new NullReferenceException("Error while getting database context.");

	while (true)
	{
		try
		{
			db.Database.Migrate();
			break;
		}
		catch (NpgsqlException)
		{
			app.Logger.LogError("Can't connect to database, retrying in 5 seconds");
			await Task.Delay(5000);
		}
	}
}

Directory.CreateDirectory(AppSettings.Storage.StoragePath);
Directory.CreateDirectory(AppSettings.Storage.TempPath);
Directory.CreateDirectory(AppSettings.Storage.DawPath);

app.Run();