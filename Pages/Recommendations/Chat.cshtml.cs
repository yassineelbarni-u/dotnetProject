using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetTestDotNet.Services;

namespace ProjetTestDotNet.Pages.Recommendations
{
    public class ChatModel : PageModel
    {
        private readonly IRecommendationService _recommendationService;

        public ChatModel(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [BindProperty]
        public string? UserMessage { get; set; }

        public string? BotResponse { get; set; }
        public bool IsLoading { get; set; } = false;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserMessage))
            {
                BotResponse = " Veuillez poser une question.";
                return Page();
            }

            IsLoading = true;
            BotResponse = await _recommendationService.GetRecommendationsAsync(UserMessage);
            IsLoading = false;
            
            return Page();
        }
    }
}
