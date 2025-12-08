using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Models;
using ProduitModel = ProjetTestDotNet.Models.Produit;
using PanierModel = ProjetTestDotNet.Models.Panier;

namespace ProjetTestDotNet.Pages.Produit
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
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

            Categories = await _context.Produits
                .Where(p => !string.IsNullOrEmpty(p.Categorie))
                .Select(p => p.Categorie!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _context.Produits.AsQueryable();
            
            if (!string.IsNullOrEmpty(Categorie))
            {
                query = query.Where(p => p.Categorie == Categorie);
            }

            Produits = await query.ToListAsync();
            

            var sessionId = HttpContext.Session.GetString("SessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                NombreArticlesPanier = await _context.Paniers
                    .Where(p => p.SessionId == sessionId)
                    .SumAsync(p => p.Quantite);
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
            TempData["Message"] = "Produit ajoute au panier avec succes !";
            
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

            var items = paniers.Select(p => new
            {
                nom = p.Produit.Nom,
                quantite = p.Quantite,
                prixUnitaire = p.PrixUnitaire,
                sousTotal = p.Quantite * p.PrixUnitaire
            }).ToList();

            var total = paniers.Sum(p => p.Quantite * p.PrixUnitaire);

            return new JsonResult(new { items, total });
        }
    }
}
