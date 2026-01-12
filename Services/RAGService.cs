using System.Text.RegularExpressions;
using ProjetTestDotNet.Models;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service RAG (Retrieval-Augmented Generation) - Partie RETRIEVAL.
    /// Responsable du filtrage intelligent des produits pertinents.
    /// 
    /// 🔍 4 FILTRES INTELLIGENTS :
    /// 1. Prix (Regex) : Détecte "moins de X€", "plus de X€", "entre X et Y€"
    /// 2. Catégorie : Recherche par nom de catégorie
    /// 3. Mots-clés : Extraction et recherche textuelle
    /// 4. Similarité sémantique : Score de pertinence
    /// 
    /// ⚠️ NOTE : Ce RAG utilise des algorithmes classiques (Regex, LINQ, scoring).
    /// Pour un RAG avancé avec embeddings vectoriels, voir documentation.
    /// </summary>
    public class RAGService : IRAGService
    {
        /// <summary>
        /// Stop words français à ignorer dans l'extraction de mots-clés
        /// </summary>
        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "le", "la", "les", "un", "une", "des", "de", "du", "à", "au", "aux",
            "je", "tu", "il", "nous", "vous", "ils", "que", "qui", "quoi", "quel",
            "est", "sont", "pour", "dans", "sur", "avec", "sans", "par", "me", "te",
            "se", "mon", "ton", "son", "ma", "ta", "sa", "mes", "tes", "ses",
            "veux", "cherche", "recommande", "propose", "donne", "montre", "trouve"
        };

        // ═══════════════════════════════════════════════════════════════════════
        // 🔍 MÉTHODE PRINCIPALE : RETRIEVAL
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Récupère les produits pertinents selon la question de l'utilisateur.
        /// Applique 4 filtres intelligents dans l'ordre de priorité.
        /// </summary>
        public List<Produit> RetrieveRelevantProducts(string userQuery, List<Produit> allProducts)
        {
            var queryLower = userQuery.ToLower();
            var relevantProducts = new List<Produit>();

            // ──────────────────────────────────────────────────────────────────
            // FILTRE 1 : Détection des critères de PRIX (priorité haute)
            // ──────────────────────────────────────────────────────────────────
            
            // Pattern : "moins de X", "< X", "inférieur à X"
            var matchMoinsDe = Regex.Match(
                queryLower, @"(?:moins de|< ?|inférieur|en dessous)\D*?(\d+)");
            
            if (matchMoinsDe.Success && int.TryParse(matchMoinsDe.Groups[1].Value, out int prixMax))
            {
                relevantProducts = allProducts.Where(p => p.Prix < prixMax).ToList();
                return relevantProducts.Any() ? relevantProducts : allProducts.Take(5).ToList();
            }

            // Pattern : "plus de X", "> X", "supérieur à X"
            var matchPlusDe = Regex.Match(
                queryLower, @"(?:plus de|> ?|supérieur|au[- ]dessus)\D*?(\d+)");
            
            if (matchPlusDe.Success && int.TryParse(matchPlusDe.Groups[1].Value, out int prixMin))
            {
                relevantProducts = allProducts.Where(p => p.Prix > prixMin).ToList();
                return relevantProducts.Any() ? relevantProducts : allProducts.Take(5).ToList();
            }

            // Pattern : "entre X et Y"
            var matchEntre = Regex.Match(
                queryLower, @"entre\D*?(\d+)\D*?(\d+)");
            
            if (matchEntre.Success && 
                int.TryParse(matchEntre.Groups[1].Value, out int min) && 
                int.TryParse(matchEntre.Groups[2].Value, out int max))
            {
                relevantProducts = allProducts.Where(p => p.Prix >= min && p.Prix <= max).ToList();
                return relevantProducts.Any() ? relevantProducts : allProducts.Take(5).ToList();
            }

            // ──────────────────────────────────────────────────────────────────
            // FILTRE 2 : Recherche par CATÉGORIE
            // ──────────────────────────────────────────────────────────────────
            var categories = allProducts
                .Select(p => p.Categorie)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct();
            
            foreach (var cat in categories)
            {
                if (queryLower.Contains(cat!.ToLower()))
                {
                    relevantProducts = allProducts.Where(p => p.Categorie == cat).ToList();
                    return relevantProducts;
                }
            }

            // ──────────────────────────────────────────────────────────────────
            // FILTRE 3 : Recherche par MOTS-CLÉS dans le nom du produit
            // ──────────────────────────────────────────────────────────────────
            var keywords = ExtractKeywords(queryLower);
            if (keywords.Any())
            {
                relevantProducts = allProducts
                    .Where(p => keywords.Any(k => p.Nom?.ToLower().Contains(k) == true))
                    .ToList();
                
                if (relevantProducts.Any())
                    return relevantProducts;
            }

            // ──────────────────────────────────────────────────────────────────
            // FILTRE 4 : Recherche SÉMANTIQUE simple (Score de similarité)
            // ──────────────────────────────────────────────────────────────────
            var scoredProducts = allProducts
                .Select(p => new
                {
                    Product = p,
                    Score = CalculateSimilarityScore(userQuery, p)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // Si des produits ont un score > 0, retourner les plus pertinents
            var topScored = scoredProducts.Where(x => x.Score > 0).Take(10).ToList();
            if (topScored.Any())
            {
                return topScored.Select(x => x.Product).ToList();
            }

            // ──────────────────────────────────────────────────────────────────
            // FALLBACK : Retourner les 15 premiers produits
            // ──────────────────────────────────────────────────────────────────
            return allProducts.Take(15).ToList();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 📊 ALGORITHME DE SIMILARITÉ (Scoring simple)
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Calcule un score de similarité entre la requête et un produit.
        /// Méthode simple : compte les mots en commun (Bag of Words).
        /// 
        /// Pour un RAG avancé, utiliser :
        /// - Embeddings vectoriels (ML.NET, Semantic Kernel)
        /// - Distance cosinus entre vecteurs
        /// - Base de données vectorielle (Qdrant, Pinecone)
        /// </summary>
        public double CalculateSimilarityScore(string query, Produit product)
        {
            var queryWords = ExtractKeywords(query.ToLower());
            var productText = $"{product.Nom} {product.Categorie}".ToLower();
            
            double score = 0;
            foreach (var word in queryWords)
            {
                if (productText.Contains(word))
                {
                    score += 1.0;
                }
            }

            return score;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 🔤 EXTRACTION DE MOTS-CLÉS
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Extrait les mots-clés importants en supprimant les stop words.
        /// </summary>
        public List<string> ExtractKeywords(string text)
        {
            return text
                .Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '\n', '\r' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !StopWords.Contains(w))
                .Distinct()
                .ToList();
        }
    }
}
