using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

public class GitHubSearchTools
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GitHubSearchTools(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        
        // Set up GitHub API headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenRouter.NET-Demo");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    [ToolMethod("Search GitHub repository for code, issues, or documentation")]
    public async Task<string> SearchRepository(
        [ToolParameter("Search query for the repository (e.g., 'streaming', 'OpenRouterClient', 'async')")] string query)
    {
        try
        {
            var results = new List<string>();
            
            // Search code in the repository
            var codeResults = await SearchCode(query);
            if (!string.IsNullOrEmpty(codeResults))
            {
                results.Add($"Code Search Results:\n{codeResults}");
            }
            
            // Search issues
            var issueResults = await SearchIssues(query);
            if (!string.IsNullOrEmpty(issueResults))
            {
                results.Add($"Issues and Discussions:\n{issueResults}");
            }
            
            // Get repository information
            var repoInfo = await GetRepositoryInfo();
            if (!string.IsNullOrEmpty(repoInfo))
            {
                results.Add($"Repository Information:\n{repoInfo}");
            }

            if (!results.Any())
            {
                return "No results found for this query.";
            }

            return string.Join("\n\n", results);
        }
        catch (Exception ex)
        {
            return $"Error searching GitHub: {ex.Message}";
        }
    }

    private async Task<string> SearchCode(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString($"repo:WilliamAvHolmberg/OpenRouter.NET {query}");
            var url = $"https://api.github.com/search/code?q={encodedQuery}&sort=indexed&order=desc";
            
            var response = await _httpClient.GetStringAsync(url);
            var searchResult = JsonSerializer.Deserialize<GitHubCodeSearchResponse>(response);

            if (searchResult?.Items?.Any() != true)
                return "";

            var results = new List<string>();
            foreach (var item in searchResult.Items.Take(3))
            {
                results.Add($"📁 {item.Path}");
                if (!string.IsNullOrEmpty(item.TextMatches?.FirstOrDefault()?.Fragment))
                {
                    var fragment = item.TextMatches.First().Fragment;
                    results.Add($"   {fragment.Trim()}");
                }
                results.Add($"   🔗 {item.HtmlUrl}");
                results.Add("");
            }

            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            return $"Error searching code: {ex.Message}";
        }
    }

    private async Task<string> SearchIssues(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString($"repo:WilliamAvHolmberg/OpenRouter.NET {query}");
            var url = $"https://api.github.com/search/issues?q={encodedQuery}&sort=updated&order=desc";
            
            var response = await _httpClient.GetStringAsync(url);
            var searchResult = JsonSerializer.Deserialize<GitHubIssueSearchResponse>(response);

            if (searchResult?.Items?.Any() != true)
                return "";

            var results = new List<string>();
            foreach (var item in searchResult.Items.Take(3))
            {
                var type = item.PullRequest != null ? "🔀 Pull Request" : "📋 Issue";
                results.Add($"{type} #{item.Number}: {item.Title}");
                if (!string.IsNullOrEmpty(item.Body))
                {
                    var body = item.Body.Length > 200 ? item.Body.Substring(0, 200) + "..." : item.Body;
                    results.Add($"   {body.Replace("\n", " ").Trim()}");
                }
                results.Add($"   🔗 {item.HtmlUrl}");
                results.Add($"   📅 Updated: {item.UpdatedAt:yyyy-MM-dd}");
                results.Add("");
            }

            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            return $"Error searching issues: {ex.Message}";
        }
    }

    private async Task<string> GetRepositoryInfo()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/WilliamAvHolmberg/OpenRouter.NET");
            var repo = JsonSerializer.Deserialize<GitHubRepository>(response);

            if (repo == null)
                return "";

            var info = new List<string>();
            info.Add($"📚 {repo.Name}: {repo.Description}");
            info.Add($"⭐ Stars: {repo.StargazersCount} | 🍴 Forks: {repo.ForksCount}");
            info.Add($"🔗 Repository: {repo.HtmlUrl}");
            if (!string.IsNullOrEmpty(repo.Homepage))
            {
                info.Add($"🏠 Homepage: {repo.Homepage}");
            }

            return string.Join("\n", info);
        }
        catch (Exception ex)
        {
            return $"Error getting repository info: {ex.Message}";
        }
    }
}

// GitHub API Response Models
public class GitHubCodeSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("items")]
    public List<GitHubCodeItem>? Items { get; set; }
}

public class GitHubCodeItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
    
    [JsonPropertyName("text_matches")]
    public List<GitHubTextMatch>? TextMatches { get; set; }
}

public class GitHubTextMatch
{
    [JsonPropertyName("fragment")]
    public string? Fragment { get; set; }
}

public class GitHubIssueSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("items")]
    public List<GitHubIssue>? Items { get; set; }
}

public class GitHubIssue
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("pull_request")]
    public object? PullRequest { get; set; }
}

public class GitHubRepository
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }
    
    [JsonPropertyName("forks_count")]
    public int ForksCount { get; set; }
    
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
    
    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }
}
