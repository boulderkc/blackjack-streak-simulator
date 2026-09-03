using MudBlazor.Services;
using BlackjackStreakSimulator.Web.Components;
using BlackjackStreakSimulator.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

// HttpClient for calling the batch Azure Function - hardcoded to Core
// Tools' local default port for now; needs to move to appsettings.json
// once there's an actual deployed Function URL to point at too.
builder.Services.AddHttpClient("BatchFunction", client =>
{
    client.BaseAddress = new Uri("http://localhost:7071/");
});

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
