using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using PanierModel = ProjetTestDotNet.Models.Panier;
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
            var sessionId = HttpContext.Session.GetString("SessionId");
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                // Cacher les articles du panier avec Redis (en utilisant des DTOs)
                var panierKey = $"Panier_{sessionId}";

                // Lecture depuis Redis
                var cachedData = await _cache.GetStringAsync(panierKey);
                List<PanierItemDTO> articles;
                
                if (cachedData == null)
                {

                    // Cache MISS charger depuis la base de donnees
                    Console.WriteLine($"REDIS CACHE MISS  Chargement panier depuis la base pour session: {sessionId.Substring(0, 8)}...");
                    
                    // Charger les entités completes depuis la base
                    var paniersDb = await _context.Paniers
                        .Include(p => p.Produit)
                        .Where(p => p.SessionId == sessionId)
                        .ToListAsync();

                    // mapping  Entité -> DTO ne garder que les donnees necessaires
                    articles = paniersDb.Select(p => new PanierItemDTO
                    {
                        Id = p.Id,
                        Quantite = p.Quantite,
                        PrixUnitaire = p.PrixUnitaire,
                        ProduitId = p.ProduitId,
                        ProduitNom = p.Produit?.Nom,
                        ProduitImage = p.Produit?.Image,
                        ProduitDescription = p.Produit?.Description,
                        ProduitStock = p.Produit?.Stock ?? 0
                    }).ToList();

                    // Serialiser le DTO plus leger que l'entite complete et mettre en cache Redis
                    var serialized = JsonSerializer.Serialize(articles);
                    await _cache.SetStringAsync(panierKey, serialized, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    });
                    Console.WriteLine($"Panier (DTO) mis en cache Redis (2 min) - {articles.Count} articles");
                }
                else
                {
                    // Cache HIT  utiliser les donnees de Redis (deja en format DTO)
                    articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new List<PanierItemDTO>();
                    Console.WriteLine($"REDIS CACHE HIT - Panier (DTO) lu depuis Redis ({articles.Count} articles)");
                }
            
                // Affecter les articles du panier et calculer le total
                ArticlesPanier = articles;
                Total = ArticlesPanier.Sum(a => a.SousTotal);
            }
            else
            {
                // Pas de session - panier vide
                ArticlesPanier = new List<PanierItemDTO>();
                Total = 0;
            }
        }

        public async Task<IActionResult> OnPostSupprimerAsync(int id)
        {
            //Recupere l’identifiant unique de la session utilisateur.
            var sessionId = HttpContext.Session.GetString("SessionId");
            var panier = await _context.Paniers.FindAsync(id);
            
            // Verifie que l’article existe et qu'il appartient bien a la session actuelle
            if (panier != null && panier.SessionId == sessionId)
            {
                _context.Paniers.Remove(panier);
                await _context.SaveChangesAsync();
                
                // Invalider le cache Redis du panier et du compteur apres suppression
                await _cache.RemoveAsync($"Panier_{sessionId}");
                await _cache.RemoveAsync($"PanierCount_{sessionId}");
                
                TempData["Message"] = "Article supprime du panier.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostModifierQuantiteAsync(int id, int quantite)
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            var panier = await _context.Paniers.FindAsync(id);
            
            if (panier != null && panier.SessionId == sessionId && quantite > 0)
            {
                panier.Quantite = quantite;
                await _context.SaveChangesAsync();
                
                // Invalider le cache Redis du panier et du compteur après modification de quantité
                await _cache.RemoveAsync($"Panier_{sessionId}");
                await _cache.RemoveAsync($"PanierCount_{sessionId}");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    Console.WriteLine($"Cache Redis invalide apres modification de quantite (session: {sessionId.Substring(0, 8)}...)");
                }
                
                // TempData["Message"] = "Quantite mise a jour";
            }

            return RedirectToPage();
        }
    }
}
