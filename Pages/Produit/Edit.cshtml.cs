using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Models;
using ProduitModel = ProjetTestDotNet.Models.Produit;

namespace ProjetTestDotNet.Pages.Produit
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditModel(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public ProduitModel Produit { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Verifier si l'admin est connecte
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            var produit = await _context.Produits.FindAsync(id);
            if (produit == null)
            {
                TempData["Error"] = "Produit introuvable.";
                return RedirectToPage("/Admin/Dashboard");
            }

            Produit = produit;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Vérifier si l'admin est connecté
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var produitToUpdate = await _context.Produits.FindAsync(Produit.Id);
            if (produitToUpdate == null)
            {
                TempData["Error"] = "Produit introuvable.";
                return RedirectToPage("/Admin/Dashboard");
            }

            produitToUpdate.Nom = Produit.Nom;
            produitToUpdate.Description = Produit.Description;
            produitToUpdate.Prix = Produit.Prix;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(_environment.WebRootPath, "images", "produits", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                produitToUpdate.Image = "/images/produits/" + fileName;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Le produit '{Produit.Nom}' a été modifié avec succès.";
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
