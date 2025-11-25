using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace OpenRouterWebApi.Tools;

public class CalculatorTools
{
    [ToolMethod("Add two numbers")]
    public int Add(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a + b;
    }

    [ToolMethod("Multiply two numbers")]
    public int Multiply(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a * b;
    }

    [ToolMethod("Subtract two numbers")]
    public int Subtract(
        [ToolParameter("First number")] int a,
        [ToolParameter("Second number")] int b)
    {
        return a - b;
    }

    [ToolMethod("Divide two numbers")]
    public double Divide(
        [ToolParameter("First number")] double a,
        [ToolParameter("Second number")] double b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero");
        
        return a / b;
    }
}

