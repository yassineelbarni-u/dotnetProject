namespace ProjetTestDotNet.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) pour un article dans le panier.
    /// Contient UNIQUEMENT les données nécessaires pour l'affichage,
    /// sans les relations complexes de l'entité Panier.
    /// </summary>
    public class PanierItemDTO
    {
        public int Id { get; set; }
        
        public int Quantite { get; set; }
        
        public decimal PrixUnitaire { get; set; }
        
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitImage { get; set; }
        public string? ProduitDescription { get; set; }
        
        public int ProduitStock { get; set; }
    
        public decimal SousTotal => PrixUnitaire * Quantite;
    }
}
