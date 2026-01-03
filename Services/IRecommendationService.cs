namespace ProjetTestDotNet.Services
{
    public interface IRecommendationService
    {
        Task<string> GetRecommendationsAsync(string userMessage);
    }
}
