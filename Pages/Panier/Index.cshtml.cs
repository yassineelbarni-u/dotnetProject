using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using PanierModel = ProjetTestDotNet.Models.Panier;

namespace ProjetTestDotNet.Pages.Panier
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<PanierModel> ArticlesPanier { get; set; } = new();
        public decimal Total { get; set; }

        public async Task OnGetAsync()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                ArticlesPanier = await _context.Paniers
                    .Include(p => p.Produit)
                    .Where(p => p.SessionId == sessionId)
                    .ToListAsync();

                Total = ArticlesPanier.Sum(p => p.PrixUnitaire * p.Quantite);
            }
            else
            {
                ArticlesPanier = new List<PanierModel>();
                Total = 0;
            }
        }

        public async Task<IActionResult> OnPostSupprimerAsync(int id)
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            var panier = await _context.Paniers.FindAsync(id);
            
            if (panier != null && panier.SessionId == sessionId)
            {
                _context.Paniers.Remove(panier);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Article supprimé du panier.";
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
                TempData["Message"] = "Quantité mise à jour.";
            }

            return RedirectToPage();
        }
    }
}
