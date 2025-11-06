using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tools;

public static class ToolRegistrationExtensions
{
    public static OpenRouterClient RegisterTool<TResult>(
        this OpenRouterClient client,
        Func<string, TResult> method,
        ToolMode mode = ToolMode.AutoExecute)
    {
        return RegisterToolInternal(client, method.Method, method.Target, null, mode);
    }
    
    public static OpenRouterClient RegisterTool<T1, TResult>(
        this OpenRouterClient client,
        Func<T1, TResult> method,
        ToolMode mode = ToolMode.AutoExecute)
    {
        return RegisterToolInternal(client, method.Method, method.Target, null, mode);
    }
    
    public static OpenRouterClient RegisterTool<T1, T2, TResult>(
        this OpenRouterClient client,
        Func<T1, T2, TResult> method,
        ToolMode mode = ToolMode.AutoExecute)
    {
        return RegisterToolInternal(client, method.Method, method.Target, null, mode);
    }
    
    public static OpenRouterClient RegisterTool<TResult>(
        this OpenRouterClient client,
        Func<string, Task<TResult>> method,
        ToolMode mode = ToolMode.AutoExecute)
    {
        return RegisterToolInternal(client, method.Method, method.Target, null, mode);
    }
    
    public static OpenRouterClient RegisterTool(
        this OpenRouterClient client,
        object instance,
        string methodName,
        ToolMode mode = ToolMode.AutoExecute)
    {
        var method = instance.GetType().GetMethod(methodName);
        if (method == null)
        {
            throw new ArgumentException($"Method '{methodName}' not found on type '{instance.GetType().Name}'");
        }
        
        return RegisterToolInternal(client, method, instance, null, mode);
    }
    
    public static OpenRouterClient RegisterClientTool(
        this OpenRouterClient client,
        string toolName,
        string description,
        object parametersSchema)
    {
        var tool = Tool.CreateFunctionTool(toolName, description, parametersSchema);
        client.RegisterTool(toolName, null, description, parametersSchema, ToolMode.ClientSide);
        return client;
    }

    /// <summary>
    /// Register a strongly-typed tool using the class-based Tool&lt;TParams, TResult&gt; pattern.
    /// </summary>
    /// <typeparam name="T">The tool class that inherits from Tool&lt;TParams, TResult&gt;</typeparam>
    public static OpenRouterClient RegisterTool<T>(this OpenRouterClient client) where T : new()
    {
        var instance = new T();

        // Use reflection to find if this is a Tool<TParams, TResult>
        var baseType = typeof(T).BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(Tool<,>))
            {
                // Found Tool<TParams, TResult>
                var genericArgs = baseType.GetGenericArguments();
                var paramsType = genericArgs[0];
                var resultType = genericArgs[1];

                // Get tool metadata
                var nameProperty = typeof(T).GetProperty("Name");
                var descriptionProperty = typeof(T).GetProperty("Description");
                var modeProperty = typeof(T).GetProperty("Mode");

                var toolName = (string)nameProperty!.GetValue(instance)!;
                var description = (string)descriptionProperty!.GetValue(instance)!;
                var mode = (ToolMode)modeProperty!.GetValue(instance)!;

                // Generate schema from TParams
                var parametersSchema = SchemaGenerator.GenerateSchema(paramsType);

                // Create implementation wrapper
                Func<string, object> implementation = (argsJson) =>
                {
                    try
                    {
                        // Call the internal ExecuteJson method
                        var executeMethod = baseType.GetMethod("ExecuteJson",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };

                        var resultJson = (string)executeMethod!.Invoke(instance, new object[] { argsJson, jsonOptions })!;

                        // If result is VoidResult, return simple success message
                        if (resultType == typeof(VoidResult))
                        {
                            return "Success";
                        }

                        // Parse and return the result
                        var result = JsonSerializer.Deserialize<object>(resultJson, jsonOptions);
                        return result!;
                    }
                    catch (Exception ex)
                    {
                        if (ex is TargetInvocationException targetEx && targetEx.InnerException != null)
                        {
                            ex = targetEx.InnerException;
                        }

                        return $"Error executing tool: {ex.Message}";
                    }
                };

                client.RegisterTool(toolName, implementation, description, parametersSchema, mode);
                return client;
            }

            baseType = baseType.BaseType;
        }

        throw new InvalidOperationException(
            $"Type '{typeof(T).Name}' does not inherit from Tool<TParams, TResult>");
    }
    
    private static OpenRouterClient RegisterToolInternal(
        OpenRouterClient client,
        MethodInfo methodInfo,
        object? targetInstance,
        string? customName = null,
        ToolMode mode = ToolMode.AutoExecute)
    {
        var toolAttr = methodInfo.GetCustomAttribute<ToolMethodAttribute>();
        if (toolAttr == null)
        {
            throw new InvalidOperationException(
                $"Method '{methodInfo.Name}' is not marked with [ToolMethod] attribute");
        }
        
        string toolName = customName ?? toolAttr.Name ?? ToSnakeCase(methodInfo.Name);
        
        var parametersSchema = SchemaGenerator.GenerateParametersSchema(methodInfo);
        
        var tool = Tool.CreateFunctionTool(
            toolName,
            toolAttr.Description,
            parametersSchema
        );
        
        Func<string, object> implementation = (argsJson) =>
        {
            try
            {
                var argValues = DeserializeArguments(argsJson, methodInfo);
                
                var result = methodInfo.Invoke(targetInstance, argValues);
                
                if (result is Task task)
                {
                    task.Wait();
                    
                    if (methodInfo.ReturnType.IsGenericType && 
                        methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        result = resultProperty!.GetValue(task);
                    }
                    else
                    {
                        result = "Task completed successfully";
                    }
                }
                
                return result!;
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException targetEx && targetEx.InnerException != null)
                {
                    ex = targetEx.InnerException;
                }
                
                return $"Error executing tool: {ex.Message}";
            }
        };
        
        client.RegisterTool(toolName, implementation, toolAttr.Description, parametersSchema, mode);
        
        return client;
    }
    
    private static object?[] DeserializeArguments(string argsJson, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return Array.Empty<object>();
        }
        
        JsonDocument jsonDoc = JsonDocument.Parse(argsJson);
        var argValues = new object?[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            
            if (jsonDoc.RootElement.TryGetProperty(param.Name!, out var jsonValue))
            {
                argValues[i] = DeserializeValue(jsonValue, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                argValues[i] = param.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Required parameter '{param.Name}' was not provided");
            }
        }
        
        return argValues;
    }
    
    private static object? DeserializeValue(JsonElement jsonValue, Type targetType)
    {
        switch (jsonValue.ValueKind)
        {
            case JsonValueKind.String:
                var stringValue = jsonValue.GetString();
                if (targetType == typeof(string))
                {
                    return stringValue;
                }
                return Convert.ChangeType(stringValue, targetType);
                
            case JsonValueKind.Number:
                if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return jsonValue.GetInt32();
                }
                else if (targetType == typeof(long) || targetType == typeof(long?))
                {
                    return jsonValue.GetInt64();
                }
                else if (targetType == typeof(float) || targetType == typeof(float?))
                {
                    return jsonValue.GetSingle();
                }
                else if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    return jsonValue.GetDouble();
                }
                else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                {
                    return jsonValue.GetDecimal();
                }
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                return jsonValue.GetBoolean();
                
            case JsonValueKind.Object:
                return JsonSerializer.Deserialize(jsonValue.GetRawText(), targetType);
                
            case JsonValueKind.Array:
                if (targetType.IsArray)
                {
                    var elementType = targetType.GetElementType()!;
                    var array = Array.CreateInstance(elementType, jsonValue.GetArrayLength());
                    
                    int index = 0;
                    foreach (var element in jsonValue.EnumerateArray())
                    {
                        array.SetValue(DeserializeValue(element, elementType), index++);
                    }
                    
                    return array;
                }
                else if (targetType.IsGenericType)
                {
                    var genericType = targetType.GetGenericTypeDefinition();
                    if (genericType == typeof(List<>))
                    {
                        var elementType = targetType.GetGenericArguments()[0];
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var list = Activator.CreateInstance(listType);
                        var addMethod = listType.GetMethod("Add");
                        
                        foreach (var element in jsonValue.EnumerateArray())
                        {
                            var value = DeserializeValue(element, elementType);
                            addMethod!.Invoke(list, new[] { value });
                        }
                        
                        return list;
                    }
                }
                break;
                
            case JsonValueKind.Null:
                return null;
        }
        
        return JsonSerializer.Deserialize(jsonValue.GetRawText(), targetType);
    }
    
    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        
        var snakeCase = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        
        return snakeCase;
    }
}

