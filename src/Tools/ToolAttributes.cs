namespace OpenRouter.NET.Tools;

[AttributeUsage(AttributeTargets.Method)]
public class ToolMethodAttribute : Attribute
{
    public string Description { get; }
    public string? Name { get; }
    
    public ToolMethodAttribute(string description)
    {
        Description = description;
        Name = null;
    }
    
    public ToolMethodAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class ToolParameterAttribute : Attribute
{
    public string Description { get; }
    public bool Required { get; }
    
    public ToolParameterAttribute(string description, bool required = true)
    {
        Description = description;
        Required = required;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute
{
}

