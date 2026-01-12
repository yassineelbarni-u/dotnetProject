#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service d'embeddings utilisant Semantic Kernel + Ollama.
    /// Convertit du texte en vecteurs (384 dimensions).
    /// </summary>
    public class SemanticKernelEmbeddingService : IEmbeddingService
    {
        private readonly Kernel _kernel;
        private readonly ITextEmbeddingGenerationService _embeddingService;

        public SemanticKernelEmbeddingService()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaTextEmbeddingGeneration("all-minilm", new Uri("http://localhost:11434"));
            
            _kernel = builder.Build();
            _embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var cleanText = text.Trim();
                if (string.IsNullOrEmpty(cleanText))
                {
                    return new float[384];
                }

                var embedding = await _embeddingService.GenerateEmbeddingAsync(cleanText);
                return embedding.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur génération embedding : {ex.Message}");
                return new float[384];
            }
        }

        public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();

            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingAsync(text);
                embeddings.Add(embedding);
            }

            return embeddings;
        }
    }
}
