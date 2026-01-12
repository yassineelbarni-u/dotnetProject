using ProjetTestDotNet.Models;


namespace ProjetTestDotNet.Services
{
    public interface IRAGService
    {
        List<Produit> RetrieveRelevantProducts(string userQuery, List<Produit> allProducts);

        double CalculateSimilarityScore(string query, Produit product);

        List<string> ExtractKeywords(string text);
    }
}
