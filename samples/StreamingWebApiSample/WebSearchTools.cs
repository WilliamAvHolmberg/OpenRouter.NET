using System.Text.Json;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

public class WebSearchTools
{
    private readonly HttpClient _httpClient;

    public WebSearchTools()
    {
        _httpClient = new HttpClient();
    }

    [ToolMethod("Search the web for information using DuckDuckGo")]
    public async Task<string> SearchWeb(
        [ToolParameter("The search query to look up")] string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://api.duckduckgo.com/?q={encodedQuery}&format=json&no_html=1&skip_disambig=1";
            
            Console.WriteLine($"=== DUCKDUCKGO REQUEST ===");
            Console.WriteLine($"Query: {query}");
            Console.WriteLine($"URL: {url}");
            Console.WriteLine($"=== END REQUEST ===");
            
            var response = await _httpClient.GetStringAsync(url);
            
            DuckDuckGoResponse? searchResult;
            try
            {
                searchResult = JsonSerializer.Deserialize<DuckDuckGoResponse>(response);
            }
            catch (JsonException ex)
            {
                // Log raw response only when deserialization fails
                Console.WriteLine($"=== JSON DESERIALIZATION ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Path: {ex.Path}");
                Console.WriteLine($"Raw Response: {response}");
                Console.WriteLine($"=== END ERROR LOG ===");
                throw;
            }

            if (searchResult == null)
                return "No search results found.";

            // Check if this is a test/empty response
            if (string.IsNullOrEmpty(searchResult.Abstract) && 
                string.IsNullOrEmpty(searchResult.AbstractText) && 
                string.IsNullOrEmpty(searchResult.Heading) &&
                (searchResult.RelatedTopics?.Count ?? 0) == 0 &&
                (searchResult.Results?.Count ?? 0) == 0)
            {
                return $"No instant answer found for '{query}'. Try rephrasing your question or asking about a more specific topic.";
            }

            var result = new List<string>();

            if (!string.IsNullOrEmpty(searchResult.Abstract))
            {
                result.Add($"Summary: {searchResult.Abstract}");
            }

            if (!string.IsNullOrEmpty(searchResult.AbstractText))
            {
                result.Add($"Details: {searchResult.AbstractText}");
            }

            if (searchResult.RelatedTopics?.Any() == true)
            {
                result.Add("Related topics:");
                foreach (var topic in searchResult.RelatedTopics.Take(3))
                {
                    if (!string.IsNullOrEmpty(topic.Text))
                    {
                        result.Add($"- {topic.Text}");
                    }
                }
            }

            if (searchResult.Results?.Any() == true)
            {
                result.Add("Web results:");
                foreach (var webResult in searchResult.Results.Take(3))
                {
                    result.Add($"- {webResult.Title}: {webResult.Snippet}");
                }
            }

            if (!result.Any())
            {
                return "No relevant information found for this query.";
            }

            return string.Join("\n\n", result);
        }
        catch (Exception ex)
        {
            return $"Error searching the web: {ex.Message}";
        }
    }
}

public class DuckDuckGoResponse
{
    public string? Abstract { get; set; }
    public string? AbstractText { get; set; }
    public string? AbstractSource { get; set; }
    public string? AbstractURL { get; set; }
    public string? Image { get; set; }
    public object? ImageHeight { get; set; }
    public object? ImageIsLogo { get; set; }
    public object? ImageWidth { get; set; }
    public string? Heading { get; set; }
    public string? Answer { get; set; }
    public string? AnswerType { get; set; }
    public string? Definition { get; set; }
    public string? DefinitionSource { get; set; }
    public string? DefinitionURL { get; set; }
    public string? Entity { get; set; }
    public object? Infobox { get; set; }
    public string? Redirect { get; set; }
    public List<RelatedTopic>? RelatedTopics { get; set; }
    public List<WebResult>? Results { get; set; }
    public string? Type { get; set; }
    public object? Meta { get; set; }
}

public class RelatedTopic
{
    public string? FirstURL { get; set; }
    public IconInfo? Icon { get; set; }
    public string? Result { get; set; }
    public string? Text { get; set; }
}

public class WebResult
{
    public string? FirstURL { get; set; }
    public IconInfo? Icon { get; set; }
    public string? Result { get; set; }
    public string? Text { get; set; }
    public string? Title { get; set; }
    public string? Snippet { get; set; }
}

public class IconInfo
{
    public string? Height { get; set; }
    public string? URL { get; set; }
    public string? Width { get; set; }
}
