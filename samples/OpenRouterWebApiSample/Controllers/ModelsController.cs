using Microsoft.AspNetCore.Mvc;
using OpenRouter.NET;

namespace OpenRouterWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly OpenRouterClient _client;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(OpenRouterClient client, ILogger<ModelsController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetModels()
    {
        try
        {
            var models = await _client.GetModelsAsync();

            var modelInfos = models.Select(m => new
            {
                m.Id,
                m.Name,
                m.ContextLength,
                Pricing = new
                {
                    m.Pricing?.Prompt,
                    m.Pricing?.Completion
                }
            }).ToList();

            return Ok(modelInfos);
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "Error fetching models");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message });
        }
    }

    [HttpGet("popular")]
    public async Task<ActionResult> GetPopularModels()
    {
        try
        {
            var models = await _client.GetModelsAsync();

            var popularModels = new[]
            {
                "openai/gpt-4o",
                "anthropic/claude-3.5-sonnet",
                "meta-llama/llama-3.1-70b-instruct",
                "google/gemini-pro"
            };

            var filtered = models
                .Where(m => popularModels.Contains(m.Id))
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ContextLength,
                    Pricing = new
                    {
                        m.Pricing?.Prompt,
                        m.Pricing?.Completion
                    }
                })
                .ToList();

            return Ok(filtered);
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "Error fetching popular models");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message });
        }
    }

    [HttpGet("limits")]
    public async Task<ActionResult> GetAccountLimits()
    {
        try
        {
            var limits = await _client.GetLimitsAsync();

            return Ok(limits);
        }
        catch (OpenRouterException ex)
        {
            _logger.LogError(ex, "Error fetching limits");
            return StatusCode(ex.StatusCode ?? 500, new { error = ex.Message });
        }
    }
}

