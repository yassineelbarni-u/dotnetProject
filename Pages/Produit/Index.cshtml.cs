using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Models;
using ProduitModel = ProjetTestDotNet.Models.Produit;
using PanierModel = ProjetTestDotNet.Models.Panier;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ProjetTestDotNet.Pages.Produit
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
        
        public List<ProduitModel> Produits { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public int NombreArticlesPanier { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? Categorie { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {

            var adminId = HttpContext.Session.GetString("AdminId");
            if (!string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            // Cacher la liste des catégories avec Redis
            var categoriesKey = "Produits_Categories";
            var cachedCategories = await _cache.GetStringAsync(categoriesKey);
            List<string> categories;
            
            if (cachedCategories == null)
            {
                categories = await _context.Produits
                    .Where(p => !string.IsNullOrEmpty(p.Categorie))
                    .Select(p => p.Categorie!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var serialized = JsonSerializer.Serialize(categories);
                await _cache.SetStringAsync(categoriesKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }
            else
            {
                categories = JsonSerializer.Deserialize<List<string>>(cachedCategories) ?? new List<string>();
            }
            Categories = categories;

           // Cacher la liste des produits selon la catégorie avec Redis
            var produitsKey = $"Produits_Categorie_{(Categorie ?? "TOUTES")}";
            var cachedProduits = await _cache.GetStringAsync(produitsKey);
            List<ProduitModel> produits;
            
            if (cachedProduits == null)
            {
                var query = _context.Produits.AsQueryable();
                if (!string.IsNullOrEmpty(Categorie))
                {
                    query = query.Where(p => p.Categorie == Categorie);
                }

                produits = await query.ToListAsync();
                var serialized = JsonSerializer.Serialize(produits);
                await _cache.SetStringAsync(produitsKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }
            else
            {
                produits = JsonSerializer.Deserialize<List<ProduitModel>>(cachedProduits) ?? new List<ProduitModel>();
            }

            Produits = produits;
            
            // Cache du nombre d'articles dans le panier avec Redis
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                var countKey = $"PanierCount_{sessionId}";
                var cachedCount = await _cache.GetStringAsync(countKey);
                int count;
                
                if (cachedCount == null)
                {
                    count = await _context.Paniers
                        .Where(p => p.SessionId == sessionId)
                        .SumAsync(p => p.Quantite);
                    await _cache.SetStringAsync(countKey, count.ToString(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    });
                }
                else
                {
                    count = int.Parse(cachedCount);
                }
                NombreArticlesPanier = count;
            }
            else
            {
                NombreArticlesPanier = 0;
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAjouterPanierAsync(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null)
            {
                return NotFound();
            }

            if (produit.Stock <= 0)
            {
                TempData["Error"] = $"Le produit '{produit.Nom}' est en rupture de stock.";
                return RedirectToPage();
            }

            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }

            var panierExistant = await _context.Paniers
                .FirstOrDefaultAsync(p => p.ProduitId == id && p.SessionId == sessionId);

            if (panierExistant != null)
            {

                if (panierExistant.Quantite >= produit.Stock)
                {
                    TempData["Error"] = $"Stock insuffisant. Maximum disponible : {produit.Stock} unité(s).";
                    return RedirectToPage();
                }
                panierExistant.Quantite++;
            }
            else
            {

                var nouveauPanier = new PanierModel
                {
                    SessionId = sessionId,
                    UserId = null,
                    ProduitId = id,
                    Quantite = 1,
                    PrixUnitaire = produit.Prix,
                    DateAjout = DateTime.Now,
                    DateExpiration = DateTime.Now.AddDays(90)
                };
                _context.Paniers.Add(nouveauPanier);
            }

            await _context.SaveChangesAsync();
            
            // Invalider le cache Redis du panier et du compteur apres ajout d'article
            await _cache.RemoveAsync($"Panier_{sessionId}");
            await _cache.RemoveAsync($"PanierCount_{sessionId}");
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCartPreviewAsync()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                return new JsonResult(new { items = new List<object>(), total = 0 });
            }

            var paniers = await _context.Paniers
                .Include(p => p.Produit)
                .Where(p => p.SessionId == sessionId)
                .ToListAsync();

            var items = paniers.Where(p => p.Produit != null).Select(p => new
            {
                nom = p.Produit!.Nom,
                quantite = p.Quantite,
                prixUnitaire = p.PrixUnitaire,
                sousTotal = p.Quantite * p.PrixUnitaire
            }).ToList();

            var total = paniers.Sum(p => p.Quantite * p.PrixUnitaire);

            return new JsonResult(new { items, total });
        }
    }
}
