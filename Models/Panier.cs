namespace ProjetTestDotNet.Models
{
    public class Panier
    {
        public int Id { get; set; }
        
        public string? SessionId { get; set; }
        
        public string? UserId { get; set; }
        
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }
        
        public int Quantite { get; set; }
        
        public decimal PrixUnitaire { get; set; }
        
        public DateTime DateAjout { get; set; }
        
        public DateTime DateExpiration { get; set; }
    }
}
