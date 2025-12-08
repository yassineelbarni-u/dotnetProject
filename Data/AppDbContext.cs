using Microsoft.EntityFrameworkCore;
using ProjetTestDotNet.Models;

namespace ProjetTestDotNet.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employe> Employes { get; set; }
        public DbSet<Produit> Produits { get; set; }
        public DbSet<Panier> Paniers { get; set; }
        public DbSet<Admin> Admins { get; set; }
    }
}
