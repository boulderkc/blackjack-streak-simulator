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

builder.Services.AddDbContext<BlackjackStreakSimulatorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BlackjackStreakSimulator"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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
