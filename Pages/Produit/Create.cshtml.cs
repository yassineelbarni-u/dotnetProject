using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Models;
using ProduitModel = ProjetTestDotNet.Models.Produit;
using Microsoft.Extensions.Caching.Memory;

namespace ProjetTestDotNet.Pages.Produit
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;

        public CreateModel(AppDbContext context, IWebHostEnvironment environment, IMemoryCache cache)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
        }

        [BindProperty]
        public ProduitModel Produit { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public void OnGet()
        {
            // Verifier si l'admin est connecte
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                Response.Redirect("/Admin/Login");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Verifier si l'admin est connecte
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(_environment.WebRootPath, "images", "produits", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                Produit.Image = "/images/produits/" + fileName;
            }

            _context.Produits.Add(Produit);
            await _context.SaveChangesAsync();
            _cache.Remove("Produits_Toutes");
            _cache.Remove("Produits_Categories");
            if (!string.IsNullOrEmpty(Produit.Categorie))
            {
                _cache.Remove($"Produits_Categorie_{Produit.Categorie}");
            }

            TempData["Message"] = $"Le produit '{Produit.Nom}' a ete ajoute avec succes !";
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
