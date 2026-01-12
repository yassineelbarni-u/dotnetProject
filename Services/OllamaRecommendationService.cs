using System.Text;
using System.Text.Json;
using ProjetTestDotNet.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service de recommandation utilisant Ollama (LLM local) avec architecture RAG.
    /// 
    /// ARCHITECTURE RAG :
    /// 1. RETRIEVAL : Délégué à RAGService (filtrage intelligent)
    /// 2. AUGMENTATION : Enrichissement du contexte (ce service)
    /// 3. GENERATION : Ollama génère la réponse (ce service)
    /// 
    /// Ce service se concentre sur la partie GÉNÉRATION du RAG.
    /// </summary>
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

        // ═══════════════════════════════════════════════════════════════════════
        // 🤖 MÉTHODE PRINCIPALE : RAG COMPLET (Retrieval + Augmentation + Generation)
        // ═══════════════════════════════════════════════════════════════════════
        
        public async Task<string> GetRecommendationsAsync(string userMessage)
        {
            try
            {
                // ──────────────────────────────────────────────────────────────
                // ÉTAPE 1 : RETRIEVAL (Récupération depuis la base de données)
                // ──────────────────────────────────────────────────────────────
                var produits = await _context.Produits.ToListAsync();

                if (!produits.Any())
                {
                    return " Aucun produit disponible dans la base de donnees.";
                }

                // ──────────────────────────────────────────────────────────────
                // ÉTAPE 2 : FILTRAGE INTELLIGENT (Délégué à RAGService)
                // ──────────────────────────────────────────────────────────────
                var produitsRelevants = _ragService.RetrieveRelevantProducts(userMessage, produits);

                // ──────────────────────────────────────────────────────────────
                // ÉTAPE 3 : AUGMENTATION (Enrichissement du contexte)
                // ──────────────────────────────────────────────────────────────
                var produitsContext = BuildProductContext(produitsRelevants);
                var statsContext = $"[Produits trouvés: {produitsRelevants.Count}/{produits.Count}]";

                // ──────────────────────────────────────────────────────────────
                // ÉTAPE 4 : GENERATION (Génération de réponse avec Ollama)
                // ──────────────────────────────────────────────────────────────
                var prompt = BuildPrompt(statsContext, produitsContext, userMessage);
                var response = await CallOllamaAsync(prompt);
                
                // Ajouter un indicateur RAG dans la réponse
                return $"{response}\n\n💡 RAG: {produitsRelevants.Count} produits analysés sur {produits.Count}";
            }
            catch (Exception ex)
            {
                return $" Erreur lors de la generation des recommandations : {ex.Message}\n\n" +
                   $" Verifiez que Ollama est demarre : `ollama serve`";
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 📝 CONSTRUCTION DU PROMPT (Augmentation)
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Construit le prompt enrichi pour le LLM.
        /// C'est la partie "Augmentation" du RAG.
        /// </summary>
        private string BuildPrompt(string stats, string productsContext, string userMessage)
        {
            return $@"Tu es un assistant e-commerce expert.

{stats}
Produits pertinents sélectionnés :
{productsContext}

Question client : {userMessage}

Instructions :
- Réponds en français précis
- Utilise UNIQUEMENT les produits listés ci-dessus
- Format : • Nom (Prix€)
- Si filtre de prix, vérifie bien le prix de chaque produit
- Maximum 5 lignes

Réponse :";
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 📋 FORMATAGE DU CONTEXTE PRODUITS
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Construit le contexte des produits pour le prompt.
        /// Format compact pour réduire les tokens.
        /// </summary>
        private string BuildProductContext(List<Models.Produit> produits)
        {
            var sb = new StringBuilder();
            var produitsLimites = produits.Take(10).ToList();

            foreach (var p in produitsLimites)
            {
                sb.AppendLine($"- {p.Nom} | {p.Prix:F0}€ | {p.Categorie ?? "Autre"}");
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 🤖 COMMUNICATION AVEC OLLAMA (LLM)
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Appelle l'API Ollama pour générer une réponse.
        /// Utilise gemma:2b avec température basse pour plus de précision.
        /// </summary>
        private async Task<string> CallOllamaAsync(string prompt)
        {
            // Créer le client HTTP via la factory
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            var requestBody = new
            {
                // ⚡ AVEC RAG : On peut utiliser gemma:2b car on envoie moins de produits
                model = "gemma:2b",        // ✅ OPTIMAL pour RAG : précis et rapide
                
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2,      // ⚡ Bas pour plus de précision (RAG nécessite moins de créativité)
                    num_predict = 150,      // Un peu plus long pour des réponses complètes
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
