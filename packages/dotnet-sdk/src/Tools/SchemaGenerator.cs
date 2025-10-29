using System.Numerics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace OpenRouter.NET.Tools;

public static class SchemaGenerator
{
    public static object GenerateParametersSchema(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        
        foreach (var param in parameters)
        {
            var paramSchema = GetParameterSchema(param);
            properties[param.Name!] = paramSchema.Schema!;
            
            var toolParamAttr = param.GetCustomAttribute<ToolParameterAttribute>();
            bool isRequired = toolParamAttr?.Required ?? !param.IsOptional;
            
            if (isRequired)
            {
                required.Add(param.Name!);
            }
        }
        
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties
        };
        
        if (required.Count > 0)
        {
            schema["required"] = required;
        }
        
        return schema;
    }
    
    private class SchemaInfo
    {
        public object? Schema { get; set; }
        public bool IsRequired { get; set; }
    }
    
    private static SchemaInfo GetParameterSchema(ParameterInfo param)
    {
        var toolParamAttr = param.GetCustomAttribute<ToolParameterAttribute>();
        var paramType = param.ParameterType;
        var isRequired = toolParamAttr?.Required ?? !param.IsOptional;
        
        var schema = new Dictionary<string, object>();
        
        if (toolParamAttr?.Description != null)
        {
            schema["description"] = toolParamAttr.Description;
        }
        
        if (paramType == typeof(string))
        {
            schema["type"] = "string";
        }
        else if (paramType == typeof(int) || paramType == typeof(long) || 
                 paramType == typeof(short) || paramType == typeof(byte) ||
                 paramType == typeof(BigInteger))
        {
            schema["type"] = "integer";
        }
        else if (paramType == typeof(float) || paramType == typeof(double) || 
                 paramType == typeof(decimal))
        {
            schema["type"] = "number";
        }
        else if (paramType == typeof(bool))
        {
            schema["type"] = "boolean";
        }
        else if (paramType.IsArray || (paramType.IsGenericType && 
                (typeof(List<>).IsAssignableFrom(paramType.GetGenericTypeDefinition()) ||
                 typeof(IEnumerable<>).IsAssignableFrom(paramType.GetGenericTypeDefinition()))))
        {
            schema["type"] = "array";
            
            Type? elementType;
            if (paramType.IsArray)
            {
                elementType = paramType.GetElementType();
            }
            else
            {
                elementType = paramType.GetGenericArguments()[0];
            }
            
            schema["items"] = GetTypeSchema(elementType!);
        }
        else if (paramType.IsClass && paramType != typeof(string))
        {
            schema["type"] = "object";
            var properties = new Dictionary<string, object>();
            var requiredProps = new List<string>();
            
            var props = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
            
            foreach (var prop in props)
            {
                var propSchema = GetPropertySchema(prop);
                if (propSchema.Schema != null)
                {
                    var jsonName = GetJsonPropertyName(prop);
                    properties[jsonName] = propSchema.Schema;
                    
                    if (propSchema.IsRequired)
                    {
                        requiredProps.Add(jsonName);
                    }
                }
            }
            
            schema["properties"] = properties;
            
            if (requiredProps.Count > 0)
            {
                schema["required"] = requiredProps;
            }
        }
        else
        {
            schema["type"] = "string";
        }
        
        return new SchemaInfo { Schema = schema, IsRequired = isRequired };
    }
    
    private static object GetTypeSchema(Type type)
    {
        var schema = new Dictionary<string, object>();
        
        if (type == typeof(string))
        {
            schema["type"] = "string";
        }
        else if (type == typeof(int) || type == typeof(long) || 
                 type == typeof(short) || type == typeof(byte) ||
                 type == typeof(BigInteger))
        {
            schema["type"] = "integer";
        }
        else if (type == typeof(float) || type == typeof(double) || 
                 type == typeof(decimal))
        {
            schema["type"] = "number";
        }
        else if (type == typeof(bool))
        {
            schema["type"] = "boolean";
        }
        else
        {
            schema["type"] = "string";
        }
        
        return schema;
    }
    
    private static SchemaInfo GetPropertySchema(PropertyInfo prop)
    {
        var jsonRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
        var jsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>() != null;
        
        if (jsonIgnore)
        {
            return new SchemaInfo { Schema = null, IsRequired = false };
        }
        
        var schema = new Dictionary<string, object>();
        var propType = prop.PropertyType;
        
        if (propType == typeof(string))
        {
            schema["type"] = "string";
        }
        else if (propType == typeof(int) || propType == typeof(long) || 
                 propType == typeof(short) || propType == typeof(byte) ||
                 propType == typeof(BigInteger))
        {
            schema["type"] = "integer";
        }
        else if (propType == typeof(float) || propType == typeof(double) || 
                 propType == typeof(decimal))
        {
            schema["type"] = "number";
        }
        else if (propType == typeof(bool))
        {
            schema["type"] = "boolean";
        }
        else if (propType.IsArray || (propType.IsGenericType && 
                (typeof(List<>).IsAssignableFrom(propType.GetGenericTypeDefinition()) ||
                 typeof(IEnumerable<>).IsAssignableFrom(propType.GetGenericTypeDefinition()))))
        {
            schema["type"] = "array";
            
            Type? elementType;
            if (propType.IsArray)
            {
                elementType = propType.GetElementType();
            }
            else
            {
                elementType = propType.GetGenericArguments()[0];
            }
            
            schema["items"] = GetTypeSchema(elementType!);
        }
        else
        {
            schema["type"] = "string";
        }
        
        return new SchemaInfo { Schema = schema, IsRequired = jsonRequired };
    }
    
    private static string GetJsonPropertyName(PropertyInfo prop)
    {
        var jsonNameAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonNameAttr != null)
        {
            return jsonNameAttr.Name;
        }
        
        return char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
    }
}

