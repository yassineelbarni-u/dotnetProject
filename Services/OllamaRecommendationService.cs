using System.Text;
using System.Text.Json;
using ProjetTestDotNet.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjetTestDotNet.Services
{
    public class OllamaRecommendationService : IRecommendationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly string _ollamaUrl = "http://localhost:11434/api/generate";

        public OllamaRecommendationService(
            IHttpClientFactory httpClientFactory,
            AppDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        public async Task<string> GetRecommendationsAsync(string userMessage)
        {
            try
            {
                var produits = await _context.Produits.ToListAsync();

                if (!produits.Any())
                {
                    return " Aucun produit disponible dans la base de donnees.";
                }

                var produitsContext = BuildProductContext(produits);

                // ⚡ PROMPT OPTIMISÉ pour petit modèle (qwen2:0.5b)
                var prompt = $@"Tu es un assistant e-commerce expert.

Produits disponibles :
{produitsContext}

Question client : {userMessage}

Instructions :
- Réponds en français (sauf si la question est en anglais)
- Maximum 3 lignes
- Recommande les produits les plus adaptés avec leur prix
- Si aucun produit ne correspond, propose une alternative";

                // Appeler l'API Ollama
                var response = await CallOllamaAsync(prompt);
                return response;
            }
            catch (Exception ex)
            {
                return $" Erreur lors de la generation des recommandations : {ex.Message}\n\n" +
                   $" Verifiez que Ollama est demarre : `ollama serve`";
            }
        }

       // Construire le contexte des produits pour le prompt
        private string BuildProductContext(List<Models.Produit> produits)
        {
            var sb = new StringBuilder();

            var produitsLimites = produits.Take(10).ToList();

            foreach (var p in produitsLimites)
            {
                // Format compact pour reduire les tokens
                sb.AppendLine($"- {p.Nom} | {p.Prix:F0}€ | {p.Categorie ?? "Autre"}");
            }
            return sb.ToString();
        }
        
        // communication avec Ollama
        private async Task<string> CallOllamaAsync(string prompt)
        {

        // Creer le client HTTP via la factory
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            var requestBody = new
            {
                // options de modele

                // model = "tinyllama",
                // model = "phi",
                model = "qwen2:0.5b",
                // model = "gemma:2b",
                
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    num_predict = 100,
                    num_ctx = 512
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);

            // Creer le contenu HTTP avec le JSON
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(_ollamaUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $" Erreur Ollama ({response.StatusCode}): {errorContent}";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                var recommendation = jsonResponse.RootElement
                    .GetProperty("response")
                    .GetString();

                return recommendation ?? "Aucune recommandation générée.";
            }
            catch (HttpRequestException ex)
            {
                return $" Impossible de se connecter à Ollama.\n\n" +
                       $" Vérifiez que Ollama est démarré :\n" +
                       $"   1. Ouvrez un terminal\n" +
                       $"   2. Exécutez : `ollama serve`\n\n" +
                       $"Erreur technique : {ex.Message}";
            }
        }
    }
}
