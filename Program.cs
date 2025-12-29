using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();

// Ajouter les sessions pour l'authentification admin
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Ajouter Redis comme cache distribué (au lieu de IMemoryCache local)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // Serveur Redis Docker
    options.InstanceName = "ProjetTestDotNet_"; // Préfixe pour les clés
});

// DbContext + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
