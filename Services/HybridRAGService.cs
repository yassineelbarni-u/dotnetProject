using ProjetTestDotNet.Models;
using System.Text.RegularExpressions;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service RAG hybride intelligent qui choisit automatiquement
    /// entre RAG classique et Vector RAG selon le type de question.
    /// 
    /// 🎯 STRATÉGIE :
    /// - Questions simples (prix, catégorie) → RAG Classique (rapide)
    /// - Questions sémantiques complexes → Vector RAG (précis)
    /// 
    /// 💡 AVANTAGES :
    /// - Optimal en performance (pas de Vector DB si pas nécessaire)
    /// - Optimal en précision (Vector DB quand utile)
    /// - Transparent pour l'utilisateur
    /// </summary>
    public class HybridRAGService : IRAGService
    {
        private readonly RAGService _classicRAG;
        private readonly VectorRAGService _vectorRAG;

        public HybridRAGService(RAGService classicRAG, VectorRAGService vectorRAG)
        {
            _classicRAG = classicRAG;
            _vectorRAG = vectorRAG;
        }

        /// <summary>
        /// Récupère les produits en choisissant intelligemment la stratégie.
        /// </summary>
        public List<Produit> RetrieveRelevantProducts(string userQuery, List<Produit> allProducts)
        {
            // STRATÉGIE 1 : Détection de questions simples → RAG Classique
            if (IsSimpleQuery(userQuery))
            {
                Console.WriteLine("🔍 RAG Classique (Regex + LINQ) - Rapide");
                return _classicRAG.RetrieveRelevantProducts(userQuery, allProducts);
            }

            // STRATÉGIE 2 : Question sémantique → Vector RAG
            Console.WriteLine("🚀 Vector RAG (Semantic Search) - Précis");
            return _vectorRAG.RetrieveRelevantProducts(userQuery, allProducts);
        }

        /// <summary>
        /// Détecte si la question est simple (filtres numériques, catégorie).
        /// </summary>
        private bool IsSimpleQuery(string query)
        {
            var lowerQuery = query.ToLower();

            // Filtres de prix (regex)
            if (Regex.IsMatch(lowerQuery, @"moins\s+de|plus\s+de|entre.*et|<|>|€|prix|coût|coute"))
                return true;

            // Filtres de catégorie (mots-clés directs)
            var categories = new[] { "livre", "formation", "console", "jeu", "development", "personnel" };
            if (categories.Any(cat => lowerQuery.Contains(cat)))
                return true;

            // Filtres de stock
            if (lowerQuery.Contains("stock") || lowerQuery.Contains("disponible"))
                return true;

            // Sinon, question sémantique complexe
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Méthodes requises par IRAGService (délégation)
        // ═══════════════════════════════════════════════════════════════════════

        public double CalculateSimilarityScore(string query, Produit product)
        {
            // Déléguer au Vector RAG pour la similarité sémantique
            return _vectorRAG.CalculateSimilarityScore(query, product);
        }

        public List<string> ExtractKeywords(string text)
        {
            // Déléguer au RAG classique pour les mots-clés
            return _classicRAG.ExtractKeywords(text);
        }
    }
}
