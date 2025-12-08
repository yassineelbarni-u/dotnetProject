using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetTestDotNet.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {

            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminUsername");
            HttpContext.Session.Clear();
            
            TempData["Message"] = "Déconnexion réussie. À bientôt !";
            return RedirectToPage("/Produit/Index");
        }
    }
}
