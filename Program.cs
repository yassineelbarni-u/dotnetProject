using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();

// Configuration des sessions
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// configuration redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "ProjetTestDotNet_";
});

// Service de recommandation IA (Ollama et RAG avec Vector Database)
builder.Services.AddHttpClient();

// Services pour Vector RAG (Qdrant + Embeddings)
builder.Services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
builder.Services.AddScoped<IQdrantService, QdrantService>();
builder.Services.AddScoped<IRAGService, VectorRAGService>();  // RAG avec Vector Database

// Service LLM (Generation)
builder.Services.AddScoped<IRecommendationService, OllamaRecommendationService>();

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

// Activer les sessions
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
