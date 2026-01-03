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

        public IndexModel(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public List<PanierItemDTO> ArticlesPanier { get; set; } = new();
        public decimal Total { get; set; }

        public async Task OnGetAsync()
        {
            // Utiliser un cookie au lieu de Session pour stocker le SessionId
            var sessionId = Request.Cookies["SessionId"];

            // Creer un SessionId si inexistant
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                Response.Cookies.Append("SessionId", sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                // Lire le panier depuis Redis uniquement
                var panierKey = $"Panier_{sessionId}";
                var cachedData = await _cache.GetStringAsync(panierKey);
                
                if (cachedData != null)
                {
                    ArticlesPanier = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
                    Console.WriteLine($"REDIS CACHE HIT - Panier lu depuis Redis ({ArticlesPanier.Count} articles)");
                }
                else
                {
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
            var panier = await _context.Paniers.FindAsync(id);
            
            // Verifie que l’article existe et qu'il appartient bien a la session actuelle
            if (panier != null && panier.SessionId == sessionId)
            {
                _context.Paniers.Remove(panier);
                await _context.SaveChangesAsync();
                
                await _cache.RemoveAsync($"Panier_{sessionId}");
                await _cache.RemoveAsync($"PanierCount_{sessionId}");
                
                TempData["Message"] = "Article supprime du panier.";
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
    }
}
