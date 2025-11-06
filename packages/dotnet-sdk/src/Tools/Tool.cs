using System.Text.Json;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tools;

/// <summary>
/// Base class for strongly-typed tools with both input and output type safety.
/// </summary>
/// <typeparam name="TParams">The parameter type for the tool</typeparam>
/// <typeparam name="TResult">The result type for the tool</typeparam>
public abstract class Tool<TParams, TResult>
{
    /// <summary>
    /// The name of the tool function.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// The execution mode for this tool. Defaults to AutoExecute.
    /// </summary>
    public virtual ToolMode Mode => ToolMode.AutoExecute;

    /// <summary>
    /// Execute the tool with typed parameters and get typed result.
    /// Use this for direct invocation and testing.
    /// </summary>
    public TResult Execute(TParams parameters)
    {
        return Handle(parameters);
    }

    /// <summary>
    /// Override this method to implement the tool's logic.
    /// </summary>
    protected abstract TResult Handle(TParams parameters);

    /// <summary>
    /// Internal method for framework to execute tool with JSON parameters.
    /// </summary>
    internal string ExecuteJson(string jsonParams, JsonSerializerOptions? options = null)
    {
        var parameters = JsonSerializer.Deserialize<TParams>(jsonParams, options);
        var result = Handle(parameters!);
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Internal method for framework to get parameter schema.
    /// </summary>
    internal object GetParameterSchema()
    {
        return SchemaGenerator.GenerateSchema(typeof(TParams));
    }
}

/// <summary>
/// Represents a void/no-result return value for tools that perform side effects.
/// </summary>
public readonly struct VoidResult
{
    /// <summary>
    /// The singleton instance of VoidResult.
    /// </summary>
    public static readonly VoidResult Instance = default;
}

/// <summary>
/// Base class for tools that don't return a value (side-effect only).
/// </summary>
/// <typeparam name="TParams">The parameter type for the tool</typeparam>
public abstract class VoidTool<TParams> : Tool<TParams, VoidResult>
{
    /// <summary>
    /// Override this method to implement the tool's logic without needing to return VoidResult.
    /// </summary>
    protected abstract void HandleVoid(TParams parameters);

    /// <summary>
    /// Final implementation that calls HandleVoid and returns VoidResult.
    /// </summary>
    protected sealed override VoidResult Handle(TParams parameters)
    {
        HandleVoid(parameters);
        return VoidResult.Instance;
    }
}
