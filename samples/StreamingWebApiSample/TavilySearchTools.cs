using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

public class TavilySearchTools
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TavilySearchTools(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    [ToolMethod("Search the web for information using Tavily AI-powered search")]
    public async Task<string> SearchWeb(
        [ToolParameter("The search query to look up")] string query)
    {
        try
        {
            var requestBody = new
            {
                query = query,
                search_depth = "basic",
                include_answer = true,
                include_images = false,
                include_raw_content = false,
                max_results = 5
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Add Bearer token authentication header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.tavily.com/search", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log the raw response for debugging
            Console.WriteLine($"=== RAW TAVILY RESPONSE ===");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Response: {responseContent}");
            Console.WriteLine($"=== END RAW RESPONSE ===");

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: Tavily API returned {response.StatusCode}: {responseContent}";
            }

            var searchResult = JsonSerializer.Deserialize<TavilyResponse>(responseContent);

            if (searchResult == null)
                return "No search results found.";

            // Debug logging
            Console.WriteLine($"=== PARSING DEBUG ===");
            Console.WriteLine($"Answer: '{searchResult.Answer}'");
            Console.WriteLine($"Results count: {searchResult.Results?.Count ?? 0}");
            Console.WriteLine($"Follow-up questions: {searchResult.FollowUpQuestions?.Count ?? 0}");
            Console.WriteLine($"=== END PARSING DEBUG ===");

            var result = new List<string>();

            if (!string.IsNullOrEmpty(searchResult.Answer))
            {
                result.Add($"Answer: {searchResult.Answer}");
            }

            if (searchResult.Results?.Any() == true)
            {
                result.Add("Search Results:");
                foreach (var webResult in searchResult.Results.Take(5))
                {
                    result.Add($"• {webResult.Title}");
                    if (!string.IsNullOrEmpty(webResult.Content))
                    {
                        result.Add($"  {webResult.Content}");
                    }
                    if (!string.IsNullOrEmpty(webResult.Url))
                    {
                        result.Add($"  Source: {webResult.Url}");
                    }
                    result.Add(""); // Empty line for readability
                }
            }

            if (!result.Any())
            {
                return "No relevant information found for this query.";
            }

            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            return $"Error searching the web: {ex.Message}";
        }
    }
}

public class TavilyResponse
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }
    
    [JsonPropertyName("follow_up_questions")]
    public List<string>? FollowUpQuestions { get; set; }
    
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }
    
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }
    
    [JsonPropertyName("results")]
    public List<TavilyResult>? Results { get; set; }
    
    [JsonPropertyName("response_time")]
    public double? ResponseTime { get; set; }
    
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }
}

public class TavilyResult
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("score")]
    public double? Score { get; set; }
    
    [JsonPropertyName("raw_content")]
    public string? RawContent { get; set; }
}
