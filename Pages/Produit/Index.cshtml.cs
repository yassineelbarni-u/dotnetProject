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

            var adminId = Request.Cookies["AdminId"];
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
            var sessionId = Request.Cookies["SessionId"];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var countKey = $"PanierCount_{sessionId}";
                var cachedCount = await _cache.GetStringAsync(countKey);
                int count;
                
                if (cachedCount == null)
                {
                    // Lire depuis Redis
                    var panierKey = $"Panier_{sessionId}";
                    var cachedData = await _cache.GetStringAsync(panierKey);
                    
                    if (cachedData != null)
                    {
                        var articles = JsonSerializer.Deserialize<List<ProjetTestDotNet.DTOs.PanierItemDTO>>(cachedData);
                        count = articles?.Sum(a => a.Quantite) ?? 0;
                    }
                    else
                    {
                        count = 0;
                    }
                    
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

            var sessionId = Request.Cookies["SessionId"];
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

            // Recuperer le panier depuis Redis
            var panierKey = $"Panier_{sessionId}";
            var cachedData = await _cache.GetStringAsync(panierKey);
            List<ProjetTestDotNet.DTOs.PanierItemDTO> articles;

            if (cachedData != null)
            {
                articles = JsonSerializer.Deserialize<List<ProjetTestDotNet.DTOs.PanierItemDTO>>(cachedData) ?? new();
            }
            else
            {
                articles = new List<ProjetTestDotNet.DTOs.PanierItemDTO>();
            }

            // Verifier si le produit existe dejà dans le panier
            var articleExistant = articles.FirstOrDefault(a => a.ProduitId == id);

            if (articleExistant != null)
            {
                if (articleExistant.Quantite >= produit.Stock)
                {
                    TempData["Error"] = $"Stock insuffisant. Maximum disponible : {produit.Stock} unité(s).";
                    return RedirectToPage();
                }
                articleExistant.Quantite++;
                articleExistant.ProduitStock = produit.Stock;
            }
            else
            {
                // Ajouter un nouvel article au panier
                articles.Add(new ProjetTestDotNet.DTOs.PanierItemDTO
                {
                    Id = articles.Count + 1,
                    ProduitId = id,
                    ProduitNom = produit.Nom,
                    ProduitImage = produit.Image,
                    ProduitDescription = produit.Description,
                    ProduitStock = produit.Stock,
                    Quantite = 1,
                    PrixUnitaire = produit.Prix
                });
            }

            var serialized = JsonSerializer.Serialize(articles);
            await _cache.SetStringAsync(panierKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });

            // Invalider le cache du compteur
            await _cache.RemoveAsync($"PanierCount_{sessionId}");
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCartPreviewAsync()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return new JsonResult(new { items = new List<object>(), total = 0 });
            }

            // Lire depuis Redis uniquement
            var panierKey = $"Panier_{sessionId}";
            var cachedData = await _cache.GetStringAsync(panierKey);
            
            if (cachedData == null)
            {
                return new JsonResult(new { items = new List<object>(), total = 0 });
            }

            var articles = JsonSerializer.Deserialize<List<ProjetTestDotNet.DTOs.PanierItemDTO>>(cachedData) ?? new();

            var items = articles.Select(a => new
            {
                nom = a.ProduitNom,
                quantite = a.Quantite,
                prixUnitaire = a.PrixUnitaire,
                sousTotal = a.SousTotal
            }).ToList();

            var total = articles.Sum(a => a.SousTotal);

            return new JsonResult(new { items, total });
        }
    }
}
