namespace ProjetTestDotNet.Models
{
    public class Categorie
    {
        public int Id { get; set; }
        public string? Nom { get; set; }
        public string? Description { get; set; }
        
        // Navigation property
        public ICollection<Produit>? Produits { get; set; }
    }
}
