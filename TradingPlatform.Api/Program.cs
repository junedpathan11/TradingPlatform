using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using TradingPlatform.Api.Hubs;
using TradingPlatform.Api.Infrastructure.Persistence;
using TradingPlatform.Api.Infrastructure.Services;
using TradingPlatform.Api.Options;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Auth API configuration =====
// Options pattern: bind the "AuthApi" section to AuthApiOptions.
// Secrets come from user-secrets automatically in Development.
builder.Services.Configure<AuthApiOptions>(
    builder.Configuration.GetSection(AuthApiOptions.SectionName));

// ===== Auth service =====
// Provider Digest quirk (see docs/api-investigation.md): the server only supports
// MD5 digest and crashes (HTTP 500 CONSTRAINT_ERROR) on other algorithm tokens.
// .NET's handler defaults to SHA-256 when the challenge omits an algorithm, so the
// Digest handshake is implemented MANUALLY in AuthService (MD5 only) instead.
//builder.Services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
//{
//    var opts = sp.GetRequiredService<IOptions<AuthApiOptions>>().Value;
//    client.BaseAddress = new Uri(opts.BaseUrl);
//    client.Timeout = TimeSpan.FromSeconds(15);
//});

//builder.Services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
//{
//    var opts = sp.GetRequiredService<IOptions<AuthApiOptions>>().Value;
//    client.BaseAddress = new Uri(opts.BaseUrl);
//    client.Timeout = TimeSpan.FromSeconds(15);

//    // Provider requirement (discovered via leaked ORA-01400 on SESSN.USER_AGENT):
//    // the server stores the User-Agent header into the session row on login and
//    // fails the login if it is missing. Handler-based clients always sent one;
//    // our manual Digest requests must too.
//    client.DefaultRequestHeaders.UserAgent.ParseAdd("TradingPlatform.Api/1.0");
//});

// ===== Auth service (Phase 2/2.5) =====
// Named HttpClient config + SINGLETON IAuthService: one token cache for the whole
// process. (Typed-client registration would make AuthService transient — every
// probe request would perform a fresh Digest login and potentially invalidate
// the feed's server session.)
builder.Services.AddHttpClient("AuthApi", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AuthApiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TradingPlatform.Api/1.0");
});

builder.Services.AddSingleton<IAuthService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var opts = sp.GetRequiredService<IOptions<AuthApiOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<AuthService>>();
    return new AuthService(factory.CreateClient("AuthApi"), Options.Create(opts), logger);
});

// ===== Price feed configuration + store (Phase 3) =====
builder.Services.Configure<FeedOptions>(
    builder.Configuration.GetSection(FeedOptions.SectionName));

// Singleton: one shared in-memory price state for the whole process —
// the feed service writes, every HTTP request reads the same instance.
builder.Services.AddSingleton<IPriceStore, InMemoryPriceStore>();

// Feed connection state (Phase 3): written by the background feed service,
// read by health/probe endpoints — one shared instance process-wide.
builder.Services.AddSingleton<FeedStateService>();

// ===== Position calculator (Phase 5, Step 22) =====
// Pure/stateless netting + PnL math — no dependencies, so a singleton avoids
// unnecessary per-request allocation.
builder.Services.AddSingleton<IPositionCalculator, PositionCalculator>();

// ===== Order validation (Phase 5, Step 24) =====
// FluentValidation, invoked manually in OrdersController (not wired into
// ASP.NET's automatic ModelState pipeline) so the existing { error: "..." }
// response shape from Step 20 is preserved exactly.
builder.Services.AddScoped<FluentValidation.IValidator<TradingPlatform.Api.Contracts.OrderRequest>,
    TradingPlatform.Api.Validators.OrderRequestValidator>();

// ===== Feed service (Phase 3) =====
// Exactly ONE feed implementation runs, chosen by Feed:Mode:
//   "Live" (default) — provider WebSocket;   "Mock" — synthetic demo ticks.
var feedOptions = builder.Configuration.GetSection(FeedOptions.SectionName).Get<FeedOptions>()
    ?? new FeedOptions();

if (string.Equals(feedOptions.Mode, "Mock", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<MockPriceFeedService>();
}
else
{
    builder.Services.AddHostedService<LivePriceFeedService>();
}


// ===== SignalR (Phase 4, Step 16) =====
// Live price streaming to the browser dashboard. Server-side SignalR ships in
// the ASP.NET Core shared framework (Microsoft.NET.Sdk.Web) — no extra NuGet
// package needed here; only the @microsoft/signalr JS client is added later
// on the frontend (Phase 6).
builder.Services.AddSignalR();

// ===== Throttled broadcast (Phase 4, Step 17) =====
// Reads IPriceStore on a 300ms cadence and pushes only changed symbols to the
// "market" group — decoupled from whichever feed service (Live/Mock) is
// currently writing into IPriceStore, so neither feed service needed changes.
builder.Services.AddHostedService<MarketBroadcastService>();

// ===== CORS (Phase 4, Step 16) =====
// The React/Vite dev server runs on a different origin than this API, and the
// SignalR hub connection uses credentials, so it needs an explicit origin
// allow-list (AllowAnyOrigin is incompatible with AllowCredentials). Default
// Vite dev ports covered for now — revisit once the frontend project's real
// dev port is confirmed in Phase 6.
const string SignalRClientCorsPolicy = "SignalRClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(SignalRClientCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

// ===== TEMPORARY AUTH CONFIG VERIFICATION =====
// Logs only whether configuration values are present.
// Never logs the actual password or credentials.
var authOptions =
    app.Services.GetRequiredService<IOptions<AuthApiOptions>>().Value;

Console.WriteLine(
    $"[startup] AuthApi bound: " +
    $"BaseUrl='{authOptions.BaseUrl}', " +
    $"TokenPath='{authOptions.TokenPath}', " +
    $"Username={(string.IsNullOrEmpty(authOptions.Username) ? "MISSING" : "set")}, " +
    $"AccountId={(string.IsNullOrEmpty(authOptions.AccountId) ? "MISSING" : "set")}, " +
    $"Password={(string.IsNullOrEmpty(authOptions.Password) ? "MISSING" : "set")}");

// Configure the HTTP request pipeline.

// ===== Global exception handling (Phase 5, Step 23) =====
// First in the pipeline so it wraps everything downstream — any unhandled
// exception becomes a consistent { error } JSON response instead of the
// framework's default HTML/stack-trace page, and is always logged server-side.
app.UseMiddleware<TradingPlatform.Api.Middleware.ExceptionHandlingMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(SignalRClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();
app.MapHub<MarketHub>("/hubs/market");

app.Run();