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
    private const string ClosingTag = "</artifact>";
    
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

