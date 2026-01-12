using System.Text;
using System.Text.Json;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service Qdrant (Vector Database) utilisant l'API REST.
    /// </summary>
    public class QdrantService : IQdrantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _qdrantUrl;

        public QdrantService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _qdrantUrl = "http://localhost:6333";
        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_qdrantUrl}/collections/{collectionName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateCollectionAsync(string collectionName, int vectorSize)
        {
            var requestBody = new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = "Cosine"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_qdrantUrl}/collections/{collectionName}", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur création collection : {error}");
            }

            Console.WriteLine($"✅ Collection '{collectionName}' créée dans Qdrant");
        }

        public async Task DeleteCollectionAsync(string collectionName)
        {
            await _httpClient.DeleteAsync($"{_qdrantUrl}/collections/{collectionName}");
        }

        public async Task UpsertAsync(
            string collectionName, 
            int id, 
            float[] vector, 
            Dictionary<string, object> payload)
        {
            var requestBody = new
            {
                points = new[]
                {
                    new
                    {
                        id = id,
                        vector = vector,
                        payload = payload
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_qdrantUrl}/collections/{collectionName}/points", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur upsert : {error}");
            }
        }

        public async Task<List<int>> SearchAsync(float[] queryVector, int topK = 10)
        {
            var requestBody = new
            {
                vector = queryVector,
                limit = topK,
                with_payload = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_qdrantUrl}/collections/products/points/search", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("⚠️ Qdrant non disponible");
                    return new List<int>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<QdrantSearchResponse>(responseContent);

                return searchResult?.result?.Select(r => r.id).ToList() ?? new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erreur Qdrant : {ex.Message}");
                return new List<int>();
            }
        }

        private class QdrantSearchResponse
        {
            public List<QdrantSearchResult>? result { get; set; }
        }

        private class QdrantSearchResult
        {
            public int id { get; set; }
            public double score { get; set; }
        }
    }
}
