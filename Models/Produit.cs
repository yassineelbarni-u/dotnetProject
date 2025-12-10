namespace ProjetTestDotNet.Models
{
    public class Produit
    {
        public int Id { get; set; }
        public string? Nom { get; set; }
        public string? Description { get; set; }
        public decimal Prix { get; set; }
        public string? Image { get; set; }
        public int Stock { get; set; } = 0;
        
        public string? Categorie { get; set; }
    }
}
