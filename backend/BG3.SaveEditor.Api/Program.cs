var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS for Angular dev server (only needed during development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",   // Angular dev server
                "http://localhost:5173",   // Vite fallback
                "http://127.0.0.1:4200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("Frontend");

// Serve the embedded Angular SPA from wwwroot
app.UseDefaultFiles();          // Serve index.html for /
app.UseStaticFiles();           // Serve JS/CSS/assets from wwwroot

app.UseHttpsRedirection();
app.MapControllers();

// SPA fallback: any non-API, non-file request returns index.html
app.MapFallbackToFile("index.html");

app.Run();
