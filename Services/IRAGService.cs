using ProjetTestDotNet.Models;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Interface pour le service RAG (Retrieval-Augmented Generation).
    /// Responsable de la partie RETRIEVAL : sélection des produits pertinents.
    /// </summary>
    public interface IRAGService
    {
        /// <summary>
        /// Récupère les produits pertinents selon la question de l'utilisateur.
        /// Utilise 4 filtres intelligents : prix, catégorie, mots-clés, similarité.
        /// </summary>
        /// <param name="userQuery">Question de l'utilisateur</param>
        /// <param name="allProducts">Liste complète des produits</param>
        /// <returns>Liste filtrée des produits pertinents</returns>
        List<Produit> RetrieveRelevantProducts(string userQuery, List<Produit> allProducts);

        /// <summary>
        /// Calcule un score de similarité entre une requête et un produit.
        /// Plus le score est élevé, plus le produit est pertinent.
        /// </summary>
        /// <param name="query">Requête utilisateur</param>
        /// <param name="product">Produit à évaluer</param>
        /// <returns>Score de similarité (0.0 = pas pertinent, >0 = pertinent)</returns>
        double CalculateSimilarityScore(string query, Produit product);

        /// <summary>
        /// Extrait les mots-clés importants d'une requête (enlève les stop words).
        /// </summary>
        /// <param name="text">Texte à analyser</param>
        /// <returns>Liste de mots-clés</returns>
        List<string> ExtractKeywords(string text);
    }
}
