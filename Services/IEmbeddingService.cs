namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Service d'embeddings vectoriels (texte → vecteur).
    /// </summary>
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts);
    }
}
