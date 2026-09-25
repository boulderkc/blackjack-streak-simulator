using MudBlazor.Services;
using BlackjackStreakSimulator.Web.Components;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.EntityFrameworkCore;
using BlackjackStreakSimulator.Data;

var builder = WebApplication.CreateBuilder(args);

// Pin the culture explicitly rather than relying on the host OS's locale -
// on Windows dev machines that's en-US by default so currency formatting
// (Format="C0" etc.) just works, but Azure App Service's Linux runtime has
// no LANG/LC_ALL set, so .NET falls back to the invariant culture there and
// renders amounts with the generic "¤" symbol instead of "$". Setting
// DefaultThreadCurrentCulture (an AppDomain-wide default for new threads)
// covers Blazor Server's circuit threads, which request-based localization
// middleware wouldn't reliably reach.
var defaultCulture = new System.Globalization.CultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// Add MudBlazor services
builder.Services.AddMudServices();

// Add app insights so we can track unique users on the azure portal
builder.Services.AddApplicationInsightsTelemetry();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Per-circuit simulation config, set on the config page and read by
// step-through and batch sim - see SimulationConfigState for why this is
// Scoped.
builder.Services.AddScoped<SimulationConfigState>();

// Per-circuit in-progress step-through session - survives navigating away
// from and back to the step-through page, same Scoped reasoning as above.
builder.Services.AddScoped<StepThroughSimulationState>();

// HttpClient for calling the batch Azure Function - base URL comes from
// config rather than being hardcoded, so local dev (appsettings.Development.json)
// and the deployed App Service (an Application Setting of the same name) can
// each point at their own Function endpoint.
string batchFunctionBaseUrl = builder.Configuration["BatchFunctionBaseUrl"]
    ?? throw new InvalidOperationException("BatchFunctionBaseUrl is not configured.");
builder.Services.AddHttpClient("BatchFunction", client =>
{
    client.BaseAddress = new Uri(batchFunctionBaseUrl);
});

// Shared between both registrations below so the connection string and
// retry policy can't drift out of sync between them.
Action<DbContextOptionsBuilder> configureDbContext = options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BlackjackStreakSimulator"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null));

// Scoped instance for ordinary per-circuit page use (BatchHistory, etc.).
// optionsLifetime: Singleton is required for AddDbContextFactory (below) to
// coexist with this - AddDbContextFactory's factory is itself a singleton,
// and a singleton can't consume a Scoped DbContextOptions<T>, which is what
// AddDbContext registers by default. This only changes the lifetime of the
// options object, not the DbContext itself - the line below still hands out
// a fresh scoped context per circuit exactly as before. Locally, ASP.NET
// Core's Development-only strict DI validation catches this mismatch at
// startup; Production skips that validation, which is why this worked
// (via an accidental captive-dependency anti-pattern) once deployed but
// crashed immediately when run locally.
builder.Services.AddDbContext<BlackjackStreakSimulatorDbContext>(configureDbContext, optionsLifetime: ServiceLifetime.Singleton);

// Factory for cases that need an independent context instance rather than
// the shared per-circuit one - specifically, MainLayout's background
// database warm-up ping, which runs fire-and-forget and must not risk
// touching the same DbContext instance a page's own query might be using
// at the same moment (DbContext isn't safe for concurrent use).
builder.Services.AddDbContextFactory<BlackjackStreakSimulatorDbContext>(configureDbContext);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
