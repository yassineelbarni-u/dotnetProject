using ProjetTestDotNet.Models;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service RAG avec Vector Database (Qdrant).
    /// Utilise des embeddings vectoriels pour la recherche sémantique.
    /// </summary>
    public class VectorRAGService : IRAGService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IQdrantService _qdrantService;
        private readonly Dictionary<int, float[]> _productEmbeddingsCache;

        public VectorRAGService(
            IEmbeddingService embeddingService,
            IQdrantService qdrantService)
        {
            _embeddingService = embeddingService;
            _qdrantService = qdrantService;
            _productEmbeddingsCache = new Dictionary<int, float[]>();
        }

        public async Task<List<Produit>> RetrieveRelevantProductsAsync(
            string userQuery, 
            List<Produit> allProducts)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(userQuery);
            await IndexProductsIfNeededAsync(allProducts);
            
            var similarProductIds = await _qdrantService.SearchAsync(queryEmbedding, topK: 10);
            
            var relevantProducts = allProducts
                .Where(p => similarProductIds.Contains(p.Id))
                .ToList();

            return relevantProducts.Any() ? relevantProducts : allProducts.Take(10).ToList();
        }

        public List<Produit> RetrieveRelevantProducts(string userQuery, List<Produit> allProducts)
        {
            return RetrieveRelevantProductsAsync(userQuery, allProducts).GetAwaiter().GetResult();
        }

        private async Task IndexProductsIfNeededAsync(List<Produit> products)
        {
            var collectionExists = await _qdrantService.CollectionExistsAsync("products");
            
            if (!collectionExists)
            {
                await _qdrantService.CreateCollectionAsync("products", vectorSize: 384);
            }

            var productsToIndex = products
                .Where(p => !_productEmbeddingsCache.ContainsKey(p.Id))
                .ToList();

            if (productsToIndex.Any())
            {
                Console.WriteLine($"🔄 Indexation de {productsToIndex.Count} produits dans Qdrant...");

                foreach (var product in productsToIndex)
                {
                    var productText = $"{product.Nom} {product.Categorie} {product.Description}";
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(productText);

                    await _qdrantService.UpsertAsync(
                        collectionName: "products",
                        id: product.Id,
                        vector: embedding,
                        payload: new Dictionary<string, object>
                        {
                            ["nom"] = product.Nom ?? "",
                            ["prix"] = product.Prix,
                            ["categorie"] = product.Categorie ?? "",
                            ["stock"] = product.Stock
                        }
                    );

                    _productEmbeddingsCache[product.Id] = embedding;
                }

                Console.WriteLine("✅ Indexation terminée !");
            }
        }

        public double CalculateSimilarityScore(string query, Produit product)
        {
            try
            {
                var queryEmbedding = _embeddingService.GenerateEmbeddingAsync(query).GetAwaiter().GetResult();
                var productText = $"{product.Nom} {product.Categorie}";
                var productEmbedding = _embeddingService.GenerateEmbeddingAsync(productText).GetAwaiter().GetResult();

                return CosineSimilarity(queryEmbedding, productEmbedding);
            }
            catch
            {
                return 0.0;
            }
        }

        private double CosineSimilarity(float[] vec1, float[] vec2)
        {
            if (vec1.Length != vec2.Length) return 0.0;

            double dotProduct = 0.0, norm1 = 0.0, norm2 = 0.0;

            for (int i = 0; i < vec1.Length; i++)
            {
                dotProduct += vec1[i] * vec2[i];
                norm1 += vec1[i] * vec1[i];
                norm2 += vec2[i] * vec2[i];
            }

            return (norm1 == 0 || norm2 == 0) ? 0.0 : dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public List<string> ExtractKeywords(string text)
        {
            return new List<string>();
        }
    }
}
