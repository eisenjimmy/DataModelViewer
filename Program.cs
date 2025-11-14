using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SchemaDiagramViewer.Services;
using ElectronNET.API;
using ElectronNET.API.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add framework services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register app services used across the app
builder.Services.AddSingleton<ExportSettingsService>();
builder.Services.AddSingleton<DatabaseConnectionService>(); // connection helper (singleton is OK for connection factory)
builder.Services.AddScoped<SchemaReaderService>();          // schema reading per scope
builder.Services.AddSingleton<DiagramService>();           // holds diagram serialization/state helpers
builder.Services.AddSingleton<PdfExportService>();         // PDF export

// Enable Electron hosting (no-op when Electron not present)
builder.WebHost.UseElectron(args);

// Configure Kestrel to listen only on localhost when running as an Electron desktop app.
// (Electron will spawn the browser window and point at the local server.)
if (HybridSupport.IsElectronActive)
{
    // Prefer an explicit localhost HTTP URL (avoids cert issues for desktop)
    builder.WebHost.UseUrls("http://127.0.0.1:5000");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Only redirect to HTTPS when NOT running under Electron (desktop).
if (!HybridSupport.IsElectronActive)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// When run under Electron, open a desktop window.
// Create the window on a background task so startup continues quickly.
if (HybridSupport.IsElectronActive)
{
    Task.Run(async () =>
    {
        var options = new BrowserWindowOptions
        {
            Width = 1200,
            Height = 900,
            Show = true,
            AutoHideMenuBar = true
        };

        var window = await Electron.WindowManager.CreateWindowAsync(options);

        // If you set UseUrls above, Electron will open the app automatically.
        window.SetTitle("Schema Diagram Viewer");

        // Optional: close the app when the window is closed
        window.OnClosed += () => Electron.App.Quit();
    });
}

app.Run();
