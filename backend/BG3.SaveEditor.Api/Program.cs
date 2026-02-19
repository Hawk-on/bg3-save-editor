using System.Net;
using System.Net.Sockets;
using Photino.NET;

// ── Logging (WinExe has no console, so also write to file) ──
var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var logFile = Path.Combine(exeDir, "bg3editor.log");
void Log(string msg)
{
    var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
    try { File.AppendAllText(logFile, line + Environment.NewLine); } catch { }
    Console.WriteLine(line);
}

// ── Cleanup handler: make sure server shuts down on any exit ──
var cts = new CancellationTokenSource();
WebApplication? webApp = null;

void Shutdown()
{
    try { cts.Cancel(); } catch { }
    try { webApp?.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); } catch { }
    try { cts.Dispose(); } catch { }
    Log("Cleanup complete");
}

AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    Log($"UNHANDLED: {e.ExceptionObject}");
    Shutdown();
};

try
{
    Log($"Starting BG3 Save Editor (exe: {exeDir})");

    bool headless = args.Contains("--no-gui");
    var port = headless ? 5062 : GetAvailablePort();
    var url = $"http://localhost:{port}";
    Log($"Mode: {(headless ? "headless" : "desktop")}, URL: {url}");

    // ── Start ASP.NET Core server on a background thread ──
    var ready = new ManualResetEventSlim(false);
    Exception? serverError = null;

    var serverThread = new Thread(() =>
    {
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = exeDir,
                WebRootPath = Path.Combine(exeDir, "wwwroot")
            });
            builder.Services.AddControllers();
            builder.WebHost.UseUrls(url);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                          .AllowAnyHeader().AllowAnyMethod());
            });

            webApp = builder.Build();
            webApp.UseCors("Frontend");
            webApp.UseDefaultFiles();
            webApp.UseStaticFiles();
            webApp.MapControllers();
            webApp.MapFallbackToFile("index.html");

            webApp.Lifetime.ApplicationStarted.Register(() =>
            {
                Log("Kestrel ready");
                ready.Set();
            });

            webApp.RunAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log($"SERVER ERROR: {ex}");
            serverError = ex;
            ready.Set();
        }
    });
    serverThread.IsBackground = true;
    serverThread.Start();

    if (!ready.Wait(TimeSpan.FromSeconds(15)))
    {
        Log("Timeout waiting for Kestrel");
        return;
    }
    if (serverError != null)
    {
        Log($"Server failed: {serverError.Message}");
        return;
    }

    Log($"Server listening on {url}");

    if (headless)
    {
        Log("Headless mode — Ctrl+C to stop");
        Thread.Sleep(Timeout.Infinite);
    }
    else
    {
        // ── Desktop mode: Photino native window ──
        // Use StartUrl property so Photino navigates when WebView2 is ready
        Log("Creating Photino window...");
        var window = new PhotinoWindow();
        window.SetTitle("BG3 Save Editor");
        window.SetSize(1280, 860);
        window.Center();
        window.SetResizable(true);
        window.SetDevToolsEnabled(true);

        // Set the URL before WaitForClose — Photino will navigate when ready
        window.StartUrl = url;

        Log($"Photino StartUrl set to {url}, calling WaitForClose...");
        window.WaitForClose();
        Log("Window closed by user");
    }
}
catch (Exception ex)
{
    Log($"FATAL: {ex}");
}
finally
{
    Shutdown();
}

static int GetAvailablePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
