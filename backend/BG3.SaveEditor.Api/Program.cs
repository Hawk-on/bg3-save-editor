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
        // NOTE: Photino.NET 4.0.16 has an AccessViolationException bug in the
        // native Photino_NavigateToUrl P/Invoke.  We work around it by loading
        // an inline HTML splash that uses JavaScript to redirect the WebView2
        // control to the Kestrel URL, bypassing the broken P/Invoke entirely.
        Log("Creating Photino window...");

        var splashHtml = $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>BG3 Save Editor</title>
<style>
  body {{ margin:0; background:#1a1a2e; display:flex; align-items:center;
         justify-content:center; height:100vh; font-family:system-ui; color:#e0e0e0; }}
  .loader {{ text-align:center; }}
  .spinner {{ width:40px; height:40px; border:4px solid #333; border-top:4px solid #6c63ff;
              border-radius:50%; animation:spin 0.8s linear infinite; margin:0 auto 16px; }}
  @keyframes spin {{ to {{ transform:rotate(360deg); }} }}
</style></head>
<body>
  <div class=""loader"">
    <div class=""spinner""></div>
    <div>Loading BG3 Save Editor…</div>
  </div>
  <script>
    // Wait briefly to ensure the server is fully responsive, then redirect.
    // Using location.replace avoids adding to the WebView2 history stack.
    setTimeout(function() {{ window.location.replace('{url}'); }}, 400);
  </script>
</body></html>";

        var window = new PhotinoWindow()
            .SetTitle("BG3 Save Editor")
            .SetSize(1280, 860)
            .Center()
            .SetResizable(true)
            .SetDevToolsEnabled(true)
            .SetLogVerbosity(1)
            .LoadRawString(splashHtml);

        Log("Photino window created, calling WaitForClose...");
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
