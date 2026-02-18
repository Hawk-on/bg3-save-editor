var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS for Angular frontend
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
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
