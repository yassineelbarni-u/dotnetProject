namespace ProjetTestDotNet.Services
{
    /// <summary>
    /// Interface pour Qdrant (Vector Database).
    /// </summary>
    public interface IQdrantService
    {
        Task<bool> CollectionExistsAsync(string collectionName);
        Task CreateCollectionAsync(string collectionName, int vectorSize);
        Task UpsertAsync(string collectionName, int id, float[] vector, Dictionary<string, object> payload);
        Task<List<int>> SearchAsync(float[] queryVector, int topK = 10);
        Task DeleteCollectionAsync(string collectionName);
    }
}
