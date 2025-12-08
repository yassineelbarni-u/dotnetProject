using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Data;
using ProjetTestDotNet.Models;
using EmployeModel = ProjetTestDotNet.Models.Employe;

namespace ProjetTestDotNet.Pages.Employe

{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }
        public List<EmployeModel> Employes { get; set; } = new();

        public async Task OnGetAsync()
        {
            Employes = await _context.Employes.ToListAsync();
        }
    }
}
