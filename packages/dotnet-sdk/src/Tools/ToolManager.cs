using System.Diagnostics;
using System.Text.Json;
using OpenRouter.NET.Models;
using OpenRouter.NET.Observability;

namespace OpenRouter.NET.Tools;

/// <summary>
/// Manages tool registration, execution, and validation
/// </summary>
internal class ToolManager
{
    private readonly List<Tool> _tools = new List<Tool>();
    private readonly Dictionary<string, Func<string, object>> _toolImplementations = new Dictionary<string, Func<string, object>>();
    private readonly Dictionary<string, ToolRegistration> _toolRegistry = new Dictionary<string, ToolRegistration>();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly OpenRouterTelemetryOptions _telemetryOptions;

    public ToolManager(JsonSerializerOptions jsonOptions, OpenRouterTelemetryOptions? telemetryOptions = null)
    {
        _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        _telemetryOptions = telemetryOptions ?? OpenRouterTelemetryOptions.Default;
    }

    /// <summary>
    /// Registers a tool with the manager
    /// </summary>
    public void RegisterTool(
        string name,
        Func<string, object>? implementation,
        string description,
        object parameters,
        ToolMode mode = ToolMode.AutoExecute)
    {
        var tool = Tool.CreateFunctionTool(name, description, parameters);
        _tools.Add(tool);

        var registration = new ToolRegistration
        {
            Name = name,
            Mode = mode,
            Handler = implementation,
            Schema = parameters
        };

        _toolRegistry[name] = registration;

        if (implementation != null && mode == ToolMode.AutoExecute)
        {
            _toolImplementations[name] = implementation;
        }
    }

    /// <summary>
    /// Executes a registered tool with the provided arguments
    /// </summary>
    public object ExecuteTool(string name, string arguments)
    {
        // Start telemetry span if enabled
        using var activity = _telemetryOptions.EnableTelemetry
            ? OpenRouterActivitySource.Instance.StartActivity(GenAiSemanticConventions.SpanNameToolCall, ActivityKind.Internal)
            : null;

        try
        {
            if (activity != null)
            {
                activity.SetTag(GenAiSemanticConventions.AttributeToolName, name);
                activity.SetTag(GenAiSemanticConventions.AttributeToolExecutionMode, "auto_execute");

                if (_telemetryOptions.CaptureToolDetails)
                {
                    var sanitized = _telemetryOptions.SanitizeToolArguments?.Invoke(arguments) ?? arguments;
                    activity.SetTag(GenAiSemanticConventions.AttributeToolArguments, sanitized);
                }
            }

            if (!_toolImplementations.TryGetValue(name, out var implementation))
            {
                throw new InvalidOperationException($"Tool '{name}' is not registered");
            }

            var validatedArguments = ValidateAndNormalizeArguments(arguments);
            var result = implementation(validatedArguments);

            // Capture result if telemetry enabled
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                if (_telemetryOptions.CaptureToolDetails && result != null)
                {
                    var resultJson = JsonSerializer.Serialize(result, _jsonOptions);
                    activity.SetTag(GenAiSemanticConventions.AttributeToolResult, resultJson);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            if (activity != null)
            {
                TelemetryHelper.RecordException(activity, ex);
                activity.SetTag(GenAiSemanticConventions.AttributeToolError, ex.Message);
            }
            throw;
        }
    }

    /// <summary>
    /// Gets the tool registration for a specific tool name
    /// </summary>
    public bool TryGetToolRegistration(string name, out ToolRegistration registration)
    {
        return _toolRegistry.TryGetValue(name, out registration!);
    }

    /// <summary>
    /// Gets all registered tools
    /// </summary>
    public List<Tool> GetAllTools() => _tools.ToList();

    /// <summary>
    /// Gets the count of registered tools
    /// </summary>
    public int ToolCount => _tools.Count;

    /// <summary>
    /// Validates and normalizes JSON arguments for tool execution
    /// </summary>
    private string ValidateAndNormalizeArguments(string arguments)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return "{}";
        }

        var trimmed = arguments.Trim();

        // Ensure JSON object or array format
        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
        {
            trimmed = "{" + trimmed + "}";
        }

        // Validate JSON
        try
        {
            using (var doc = JsonDocument.Parse(trimmed)) { }
            return trimmed;
        }
        catch (JsonException)
        {
            // Attempt to repair JSON
            return EnsureValidJson(trimmed);
        }
    }

    /// <summary>
    /// Attempts to repair invalid JSON by balancing braces
    /// </summary>
    private string EnsureValidJson(string json)
    {
        int openBraces = json.Count(c => c == '{');
        int closeBraces = json.Count(c => c == '}');

        if (openBraces > closeBraces)
        {
            json += new string('}', openBraces - closeBraces);
        }

        return json;
    }
}
