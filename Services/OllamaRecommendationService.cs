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
        private readonly IRAGService _ragService;
        private readonly string _ollamaUrl = "http://localhost:11434/api/generate";


        public OllamaRecommendationService(
            IHttpClientFactory httpClientFactory,
            AppDbContext context,
            IRAGService ragService)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _ragService = ragService;
        }


        public async Task<string> GetRecommendationsAsync(string userMessage)
        {
            try
            {
                // Récupérer tous les produits depuis la base de données
                var produits = await _context.Produits.ToListAsync();


                if (!produits.Any())
                {
                    return " Aucun produit disponible dans la base de donnees.";
                }

               // Recuperer les produits pertinents en utilisant le service RAG
                var produitsRelevants = _ragService.RetrieveRelevantProducts(userMessage, produits);


                var produitsContext = BuildProductContext(produitsRelevants);
                var statsContext = $"[Produits trouves: {produitsRelevants.Count}/{produits.Count}]";


                var prompt = BuildPrompt(statsContext, produitsContext, userMessage);
                var response = await CallOllamaAsync(prompt);
                
                return $"{response}";
            }
            catch (Exception ex)
            {
                return $" Erreur lors de la generation des recommandations : {ex.Message}\n\n" +
                   $" Verifiez que Ollama est demarre : `ollama serve`";
            }
        }


        private string BuildPrompt(string stats, string productsContext, string userMessage)
        {
            return $@"Tu es un assistant e-commerce expert.


{stats}
Produits pertinents sélectionnés :
{productsContext}


Question client : {userMessage}


Instructions :
- Réponds en français si la question en anglais repondre en anglais
- Utilise UNIQUEMENT les produits listés ci-dessus
- Format : • Nom (Prix€)
- Si filtre de prix, vérifie bien le prix de chaque produit
- Maximum 5 lignes


Réponse :";
        }


        private string BuildProductContext(List<Models.Produit> produits)
        {
            var sb = new StringBuilder();
            var produitsLimites = produits.Take(20).ToList();


            foreach (var p in produitsLimites)
            {
                sb.AppendLine($"- {p.Nom} | {p.Prix:F0}€ | {p.Categorie ?? "Autre"}");
            }
            return sb.ToString();
        }


        private async Task<string> CallOllamaAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);


            var requestBody = new
            {
                model = "gemma:2b",
                
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2,
                    num_predict = 150,
                    num_ctx = 512
                }
            };


            var jsonContent = JsonSerializer.Serialize(requestBody);


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
