using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;

namespace ProjetTestDotNet.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == Username && a.Password == Password);

            if (admin == null)
            {
                TempData["Error"] = "Identifiants incorrects.";
                return Page();
            }

            HttpContext.Session.SetString("AdminId", admin.Id.ToString());
            HttpContext.Session.SetString("AdminUsername", admin.Username ?? "");

            TempData["Message"] = $"Bienvenue Admin {admin.Username} !";
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
