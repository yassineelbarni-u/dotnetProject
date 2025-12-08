using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProduitModel = ProjetTestDotNet.Models.Produit;


namespace ProjetTestDotNet.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ProduitModel> Produits { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Verifier si l'admin est connecte
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            Produits = await _context.Produits.OrderByDescending(p => p.Id).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {

            // Verifier si l'admin est connecte
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            var produit = await _context.Produits.FindAsync(id);
            if (produit != null)
            {
                _context.Produits.Remove(produit);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Le produit '{produit.Nom}' a ete supprime avec succes";
            }

            return RedirectToPage();
        }
    }
}
