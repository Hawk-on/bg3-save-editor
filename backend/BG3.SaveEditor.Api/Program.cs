using System.Net;
using System.Net.Sockets;
using Photino.NET;

// --no-gui flag: run as a headless server (for dev / CI)
bool headless = args.Contains("--no-gui");

// Pick a free port (or use the fixed dev port in headless mode)
var port = headless ? 5062 : GetAvailablePort();
var url = $"http://localhost:{port}";

// ── Build & start the ASP.NET Core server on a background thread ──
var ready = new ManualResetEventSlim(false);
var cts = new CancellationTokenSource();

var serverThread = new Thread(() =>
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllers();
    builder.WebHost.UseUrls(url);

    // CORS only needed when Angular dev server runs separately
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://127.0.0.1:4200"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();
    app.UseCors("Frontend");
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    app.Lifetime.ApplicationStarted.Register(() => ready.Set());
    app.RunAsync(cts.Token).GetAwaiter().GetResult();
});
serverThread.IsBackground = true;
serverThread.Start();

// Wait for Kestrel to be ready (max 10 s)
if (!ready.Wait(TimeSpan.FromSeconds(10)))
{
    Console.Error.WriteLine("Server failed to start within 10 seconds.");
    return;
}

Console.WriteLine($"Server listening on {url}");

if (headless)
{
    // Headless mode: block until Ctrl+C
    Console.WriteLine("Running in headless mode (--no-gui). Press Ctrl+C to stop.");
    var exit = new ManualResetEventSlim(false);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; exit.Set(); };
    exit.Wait();
}
else
{
    // ── Desktop mode: open a native webview window ──
    var window = new PhotinoWindow()
        .SetTitle("BG3 Save Editor")
        .SetSize(1280, 860)
        .Center()
        .SetResizable(true)
        .Load(url);

    // Blocks until user closes the window
    window.WaitForClose();
}

// Shut down the server cleanly
cts.Cancel();

static int GetAvailablePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
