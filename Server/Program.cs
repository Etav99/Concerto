global using Dto = Concerto.Shared.Models.Dto;
using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Hubs;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Server.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Concerto.Server Builder");
// Add services to the container.

builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ForumService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<IdentityManagerService>();

//builder.Services.AddResponseCompression(opts =>
//{
//    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//        new[] { "application/octet-stream" });
//});

builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		}
	)
	.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.RequireHttpsMetadata = false;
			// options.RequireHttpsMetadata = builder.Environment.IsProduction();

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
							logger.LogDebug("Token set from query");
							context.Token = accessToken;
						}
					}

					return Task.CompletedTask;
				}
			};
		}
	);

builder.Services.AddAuthorization();


// Configure database context
builder.Services.AddDbContext<AppDataContext>(options =>
	options.UseNpgsql(AppSettings.Database.DbString)
);
builder.Services.AddScoped<AppDataContext>();


var app = builder.Build();

app.UsePathBase("/Concerto");

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor API V1"); });


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
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
app.MapFallbackToFile("index.html");

await using var scope = app.Services.CreateAsyncScope();
await using (var db = scope.ServiceProvider.GetService<AppDataContext>())
{
	if (db == null) throw new NullReferenceException("Error while getting database context.");
	var success = false;
	while (!success)
		try
		{
			db.Database.Migrate();
			success = true;
		}
		catch (NpgsqlException)
		{
			logger.LogError("Can't connect to database, retrying in 5 seconds");
			await Task.Delay(5000);
		}
}

app.Run();
