using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetTestDotNet.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ProjetTestDotNet.DTOs;

namespace ProjetTestDotNet.Pages.Panier
{

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        // Constructeur pour injection de dependances
        public IndexModel(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Liste des articles dans le panier
        public List<PanierItemDTO> ArticlesPanier { get; set; } = new();
        public decimal Total { get; set; }

        public async Task OnGetAsync()
        {
            // Recuperer l'identifiant unique depuis le cookie
            var sessionId = Request.Cookies["SessionId"];

            // Creer un SessionId si inexistant
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                Response.Cookies.Append("SessionId", sessionId, new CookieOptions
                {
                    HttpOnly = true, // Accessible uniquement via HTTP
                    Secure = true,
                    SameSite = SameSiteMode.Lax, // Protection CSRF
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                // Lire le panier depuis Redis
                var panierKey = $"Panier_{sessionId}";
                // recuperer les donnees serialisees JSON
                var cachedData = await _cache.GetStringAsync(panierKey);
                
                if (cachedData != null)
                {
                    // Deserialiser les donnees JSON en liste d'articles (C# OBJECT)
                    ArticlesPanier = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
                    Console.WriteLine($"REDIS CACHE HIT - Panier lu depuis Redis ({ArticlesPanier.Count} articles)");
                }
                else
                {
                    // Panier vide
                    ArticlesPanier = new List<PanierItemDTO>();
                    Console.WriteLine($"Panier vide pour session: {sessionId.Substring(0, 8)}...");
                }
            
                Total = ArticlesPanier.Sum(a => a.SousTotal);
            }
            else
            {
                ArticlesPanier = new List<PanierItemDTO>();
                Total = 0;
            }
        }

        public async Task<IActionResult> OnPostSupprimerAsync(int id)
        {
            //Recupere l'identifiant unique depuis le cookie.
            var sessionId = Request.Cookies["SessionId"];
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                // constructeur de la cle Redis
                var panierKey = $"Panier_{sessionId}";
                // Lire le panier depuis Redis
                var cachedData = await _cache.GetStringAsync(panierKey);
                
                if (cachedData != null)
                {
                    var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
                    
                    // Supprimer l'article
                    var article = articles.FirstOrDefault(a => a.Id == id);
                    if (article != null)
                    {
                        articles.Remove(article);
                        
                        // Mettre à jour Redis
                        var serialized = JsonSerializer.Serialize(articles);
                        await _cache.SetStringAsync(panierKey, serialized, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                        });
                        
                        await _cache.RemoveAsync($"PanierCount_{sessionId}");
                        
                        TempData["Message"] = "Article supprime du panier.";
                    }
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostModifierQuantiteAsync(int id, int quantite)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId) || quantite <= 0)
            {
                return RedirectToPage();
            }
            
            // Lire le panier depuis Redis
            var panierKey = $"Panier_{sessionId}";
            var cachedData = await _cache.GetStringAsync(panierKey);
            
            if (cachedData != null)
            {
                var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
                
                // Modifier la quantite
                var article = articles.FirstOrDefault(a => a.Id == id);
                if (article != null)
                {
                    article.Quantite = quantite;
                    
                    var serialized = JsonSerializer.Serialize(articles);
                    await _cache.SetStringAsync(panierKey, serialized, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                    });
                    
                    await _cache.RemoveAsync($"PanierCount_{sessionId}");
                    
                    Console.WriteLine($"Cache Redis invalide apres modification de quantite (session: {sessionId.Substring(0, 8)}...)");
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDebugAsync()
        {
            var sessionId = Request.Cookies["SessionId"];
            
            var result = "debug redis\n";
            
            if (string.IsNullOrEmpty(sessionId))
            {
                result += "Pas de sessionId\n";
                return Content(result, "text/plain; charset=utf-8");
            }
            
            
            try
            {
                var panierKey = $"Panier_{sessionId}";
                var cachedData = await _cache.GetStringAsync(panierKey);
                
                if (cachedData != null)
                {
                    var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData);
                    
                    if (articles != null && articles.Any())
                    {
                    
                        
                        foreach (var article in articles)
                        {
                            result += $" {article.ProduitNom}\n";
                            result += $"    ProduitId: {article.ProduitId}\n";
                            result += $"    Quantité: {article.Quantite}\n";
                            result += $"    Prix unitaire: {article.PrixUnitaire:F2}€\n";
                            result += $"    Sous-total: {article.SousTotal:F2}€\n";
                            result += $"    Stock disponible: {article.ProduitStock}\n\n";
                        }
                        
            
                    }
                    else
                    {
                        result += " Panier vide\n";
                    }
                }
                else
                {
                    result += "   Ajoutez des produits au panier\n";
                }
            }
            catch (Exception ex)
            {
                result += $"ERREUR: {ex.Message}\n";
                result += $"Type: {ex.GetType().Name}\n";
            }
            
            return Content(result, "text/plain; charset=utf-8");
        }
    }
}
