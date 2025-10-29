using System.Text;
using System.Text.RegularExpressions;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Parsing;

public class ArtifactParser
{
    private static readonly Regex ArtifactRegex = new(
        @"<artifact\b(?<attrs>[^>]*)>(?<content>.*?)</artifact>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    
    private readonly StringBuilder _buffer = new();
    private ParserState _state = ParserState.Normal;
    private string? _currentArtifactId;
    private string? _currentType;
    private string? _currentTitle;
    private string? _currentLanguage;
    private readonly StringBuilder _artifactContent = new();
    private int _artifactCounter = 0;
    private const string ClosingTag = "</artifact>";
    
    private static string? ExtractAttribute(string attributesString, string attributeName)
    {
        var match = Regex.Match(attributesString, 
            $@"\b{attributeName}\s*=\s*""([^""]*)""", 
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
    
    public ParseResult Parse(string input)
    {
        var result = new ParseResult();
        var matches = ArtifactRegex.Matches(input);
        var textWithoutArtifacts = input;
        
        foreach (Match match in matches)
        {
            var attrs = match.Groups["attrs"].Value;
            var id = ExtractAttribute(attrs, "id");
            var type = ExtractAttribute(attrs, "type") ?? "code";
            var title = ExtractAttribute(attrs, "title") ?? "Untitled";
            var language = ExtractAttribute(attrs, "language");
            
            var artifact = new Artifact
            {
                Id = !string.IsNullOrWhiteSpace(id) ? id : $"art_{Guid.NewGuid().ToString("N")[..8]}",
                Type = type,
                Title = title,
                Language = language,
                Content = match.Groups["content"].Value
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
                    // No <artifact found in buffer
                    // Check if buffer might contain the start of a tag that will complete in next chunk
                    bool mightBePartialTag = false;

                    for (int i = 1; i <= Math.Min(9, bufferText.Length); i++)
                    {
                        if ("<artifact".StartsWith(bufferText.Substring(bufferText.Length - i)))
                        {
                            mightBePartialTag = true;
                            var textBeforePartial = bufferText.Substring(0, bufferText.Length - i);
                            if (textBeforePartial.Length > 0)
                            {
                                result.TextDelta += textBeforePartial;
                            }
                            _buffer.Clear();
                            _buffer.Append(bufferText.Substring(bufferText.Length - i));
                            break;
                        }
                    }

                    if (!mightBePartialTag)
                    {
                        // No partial tag, emit everything as text
                        result.TextDelta += bufferText;
                        _buffer.Clear();
                    }
                    break;
                }

                // Found <artifact, extract any text before it
                if (startIdx > 0)
                {
                    result.TextDelta += bufferText.Substring(0, startIdx);
                    bufferText = bufferText.Substring(startIdx);
                    _buffer.Clear();
                    _buffer.Append(bufferText);
                }

                // Now check if we have the complete opening tag (with closing >)
                var closeTagIdx = bufferText.IndexOf('>');
                if (closeTagIdx == -1)
                {
                    // Incomplete opening tag - keep entire buffer and wait for more chunks
                    // DO NOT emit as TextDelta, DO NOT process further
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
                var endIdx = bufferText.IndexOf(ClosingTag);
                
                if (endIdx == -1)
                {
                    // Still inside artifact; we must retain a suffix to detect a closing tag
                    // that might be split across chunks. Keep ClosingTag.Length - 1 characters.
                    var keepLen = ClosingTag.Length - 1;
                    if (bufferText.Length <= keepLen)
                    {
                        // Not enough content to safely emit; keep entire buffer
                        _buffer.Clear();
                        _buffer.Append(bufferText);
                        break;
                    }

                    var emitLen = bufferText.Length - keepLen;
                    var emitText = bufferText.Substring(0, emitLen);
                    _artifactContent.Append(emitText);
                    result.ArtifactContent = new ArtifactContent(
                        _currentArtifactId!,
                        _currentType!,
                        emitText
                    );

                    _buffer.Clear();
                    _buffer.Append(bufferText.Substring(emitLen));
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
                
                bufferText = bufferText.Substring(endIdx + ClosingTag.Length);
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
        var idMatch = Regex.Match(tag, @"id=""([^""]+)""");
        var typeMatch = Regex.Match(tag, @"type=""([^""]+)""");
        var titleMatch = Regex.Match(tag, @"title=""([^""]+)""");
        var languageMatch = Regex.Match(tag, @"language=""([^""]+)""");
        
        _currentArtifactId = idMatch.Success && !string.IsNullOrWhiteSpace(idMatch.Groups[1].Value)
            ? idMatch.Groups[1].Value
            : $"art_{++_artifactCounter}";
        _currentType = typeMatch.Success ? typeMatch.Groups[1].Value : "code";
        _currentTitle = titleMatch.Success ? titleMatch.Groups[1].Value : "Untitled";
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

