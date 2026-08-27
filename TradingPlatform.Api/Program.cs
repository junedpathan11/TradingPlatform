using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradingPlatform.Api.Infrastructure.Persistence;
using TradingPlatform.Api.Infrastructure.Services;
using TradingPlatform.Api.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Auth API configuration (Phase 2) =====
// Options pattern: bind the "AuthApi" section to AuthApiOptions. Secrets come from
// user-secrets automatically in Development (they merge over appsettings.json).
builder.Services.Configure<AuthApiOptions>(
    builder.Configuration.GetSection(AuthApiOptions.SectionName));

// ===== Auth service (Phase 2) =====
// Typed HttpClient pattern: IAuthService + AuthService registered together,
// with an HttpClient pre-configured to the provider base URL. 15s timeout so a
// hanging provider fails fast instead of stalling callers.
builder.Services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AuthApiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// TEMPORARY (Phase 2 verification): log whether auth config bound — masked, never the raw secret.
var authOptions = app.Services.GetRequiredService<IOptions<AuthApiOptions>>().Value;
Console.WriteLine(
    $"[startup] AuthApi bound: BaseUrl='{authOptions.BaseUrl}', TokenPath='{authOptions.TokenPath}', " +
    $"Username={(string.IsNullOrEmpty(authOptions.Username) ? "MISSING" : "set")}, " +
    $"Password={(string.IsNullOrEmpty(authOptions.Password) ? "MISSING" : "set")}");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
