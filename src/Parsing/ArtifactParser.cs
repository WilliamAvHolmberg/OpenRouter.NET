using System.Text;
using System.Text.RegularExpressions;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Parsing;

public class ArtifactParser
{
    private readonly StringBuilder _buffer = new();
    private ParserState _state = ParserState.Normal;
    private string? _currentArtifactId;
    private string? _currentType;
    private string? _currentTitle;
    private string? _currentLanguage;
    private readonly StringBuilder _artifactContent = new();
    private int _artifactCounter = 0;
    
    public ParseResult Parse(string input)
    {
        var result = new ParseResult();
        var regex = new Regex(@"<artifact\s+type=""([^""]+)""\s+title=""([^""]+)""(?:\s+language=""([^""]+)"")?\s*>(.*?)</artifact>", RegexOptions.Singleline);
        
        var matches = regex.Matches(input);
        var textWithoutArtifacts = input;
        
        foreach (Match match in matches)
        {
            var artifact = new Artifact
            {
                Id = $"art_{Guid.NewGuid().ToString("N")[..8]}",
                Type = match.Groups[1].Value,
                Title = match.Groups[2].Value,
                Language = match.Groups[3].Success ? match.Groups[3].Value : null,
                Content = match.Groups[4].Value
            };
            
            result.Artifacts.Add(artifact);
            textWithoutArtifacts = textWithoutArtifacts.Replace(match.Value, "");
        }
        
        result.TextWithoutArtifacts = textWithoutArtifacts;
        return result;
    }
    
    public IncrementalParseResult ParseIncremental(string chunk)
    {
        var result = new IncrementalParseResult();
        _buffer.Append(chunk);
        var bufferText = _buffer.ToString();
        
        while (true)
        {
            if (_state == ParserState.Normal)
            {
                var startIdx = bufferText.IndexOf("<artifact");
                
                if (startIdx == -1)
                {
                    // Check if buffer ends with potential start of tag
                    if (bufferText.Length > 0 && bufferText.EndsWith("<") ||
                        bufferText.Length > 1 && bufferText.EndsWith("<a") ||
                        bufferText.Length > 2 && bufferText.EndsWith("<ar") ||
                        bufferText.Length > 3 && bufferText.EndsWith("<art") ||
                        bufferText.Length > 4 && bufferText.EndsWith("<artif") ||
                        bufferText.Length > 5 && bufferText.EndsWith("<artifa") ||
                        bufferText.Length > 6 && bufferText.EndsWith("<artifac"))
                    {
                        // Keep potential tag start in buffer
                        var potentialTagStart = bufferText.Length;
                        for (int i = Math.Min(8, bufferText.Length); i > 0; i--)
                        {
                            if ("<artifact".StartsWith(bufferText.Substring(bufferText.Length - i)))
                            {
                                potentialTagStart = bufferText.Length - i;
                                break;
                            }
                        }
                        
                        if (potentialTagStart > 0)
                        {
                            result.TextDelta += bufferText.Substring(0, potentialTagStart);
                            _buffer.Clear();
                            _buffer.Append(bufferText.Substring(potentialTagStart));
                        }
                    }
                    else
                    {
                        result.TextDelta += bufferText;
                        _buffer.Clear();
                    }
                    break;
                }
                
                if (startIdx > 0)
                {
                    result.TextDelta += bufferText.Substring(0, startIdx);
                    bufferText = bufferText.Substring(startIdx);
                    _buffer.Clear();
                    _buffer.Append(bufferText);
                }
                
                var closeTagIdx = bufferText.IndexOf('>');
                if (closeTagIdx == -1)
                {
                    // Incomplete opening tag
                    break;
                }
                
                var openingTag = bufferText.Substring(0, closeTagIdx + 1);
                ParseOpeningTag(openingTag);
                
                result.ArtifactStarted = new ArtifactStarted(
                    _currentArtifactId!,
                    _currentType!,
                    _currentTitle!,
                    _currentLanguage
                );
                
                bufferText = bufferText.Substring(closeTagIdx + 1);
                _buffer.Clear();
                _buffer.Append(bufferText);
                _state = ParserState.InArtifact;
            }
            
            if (_state == ParserState.InArtifact)
            {
                var endIdx = bufferText.IndexOf("</artifact>");
                
                if (endIdx == -1)
                {
                    // Still inside artifact, accumulate content
                    if (bufferText.Length > 0)
                    {
                        _artifactContent.Append(bufferText);
                        result.ArtifactContent = new ArtifactContent(
                            _currentArtifactId!,
                            _currentType!,
                            bufferText
                        );
                        _buffer.Clear();
                    }
                    break;
                }
                
                // Found closing tag
                var remainingContent = bufferText.Substring(0, endIdx);
                _artifactContent.Append(remainingContent);
                
                if (remainingContent.Length > 0)
                {
                    result.ArtifactContent = new ArtifactContent(
                        _currentArtifactId!,
                        _currentType!,
                        remainingContent
                    );
                }
                
                result.ArtifactCompleted = new ArtifactCompleted(
                    _currentArtifactId!,
                    _currentType!,
                    _currentTitle!,
                    _artifactContent.ToString(),
                    _currentLanguage
                );
                
                bufferText = bufferText.Substring(endIdx + "</artifact>".Length);
                _buffer.Clear();
                _buffer.Append(bufferText);
                
                _artifactContent.Clear();
                _state = ParserState.Normal;
                _currentArtifactId = null;
                _currentType = null;
                _currentTitle = null;
                _currentLanguage = null;
            }
        }
        
        return result;
    }
    
    private void ParseOpeningTag(string tag)
    {
        var typeMatch = Regex.Match(tag, @"type=""([^""]+)""");
        var titleMatch = Regex.Match(tag, @"title=""([^""]+)""");
        var languageMatch = Regex.Match(tag, @"language=""([^""]+)""");
        
        _currentArtifactId = $"art_{++_artifactCounter}";
        _currentType = typeMatch.Success ? typeMatch.Groups[1].Value : "unknown";
        _currentTitle = titleMatch.Success ? titleMatch.Groups[1].Value : "untitled";
        _currentLanguage = languageMatch.Success ? languageMatch.Groups[1].Value : null;
    }
}

public class ParseResult
{
    public List<Artifact> Artifacts { get; set; } = new();
    public string TextWithoutArtifacts { get; set; } = "";
}

public class IncrementalParseResult
{
    public string? TextDelta { get; set; }
    public ArtifactStarted? ArtifactStarted { get; set; }
    public ArtifactContent? ArtifactContent { get; set; }
    public ArtifactCompleted? ArtifactCompleted { get; set; }
}

enum ParserState
{
    Normal,
    InArtifact
}

