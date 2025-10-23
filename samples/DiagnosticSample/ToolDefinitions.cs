using OpenRouter.NET.Tools;

namespace DiagnosticSample;

/// <summary>
/// Contains tool method definitions for diagnostic tests
/// </summary>
public static class ToolDefinitions
{
    [ToolMethod("get_weather", "Get current weather for a city")]
    public static string GetWeather(
        [ToolParameter("The city name", required: true)] string city)
    {
        Thread.Sleep(1000); // Add delay to see the indicator
        var random = new Random();
        var temp = random.Next(-10, 35);
        var conditions = new[] { "sunny", "cloudy", "rainy", "partly cloudy" };
        var condition = conditions[random.Next(conditions.Length)];
        return $"Weather in {city}: {temp}°C, {condition}";
    }

    [ToolMethod("calculate", "Perform mathematical calculations")]
    public static string Calculate(
        [ToolParameter("Mathematical expression", required: true)] string expression)
    {
        Thread.Sleep(800); // Add delay to see the indicator
        try
        {
            expression = expression.Replace(" ", "");
            if (expression.Contains('+'))
            {
                var parts = expression.Split('+');
                return $"{expression} = {double.Parse(parts[0]) + double.Parse(parts[1])}";
            }
            else if (expression.Contains('*'))
            {
                var parts = expression.Split('*');
                return $"{expression} = {double.Parse(parts[0]) * double.Parse(parts[1])}";
            }
            return $"Could not parse: {expression}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
