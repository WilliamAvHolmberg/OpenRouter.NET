using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

// Calculator Tools - Typed Pattern

public class AddParams
{
    public int A { get; set; }
    public int B { get; set; }
}

public class AddTool : Tool<AddParams, int>
{
    public override string Name => "add";
    public override string Description => "Add two numbers together";

    protected override int Handle(AddParams p) => p.A + p.B;
}

public class SubtractTool : Tool<AddParams, int>
{
    public override string Name => "subtract";
    public override string Description => "Subtract second number from first";

    protected override int Handle(AddParams p) => p.A - p.B;
}

public class MultiplyTool : Tool<AddParams, int>
{
    public override string Name => "multiply";
    public override string Description => "Multiply two numbers";

    protected override int Handle(AddParams p) => p.A * p.B;
}

public class DivideParams
{
    public double A { get; set; }
    public double B { get; set; }
}

public class DivideTool : Tool<DivideParams, double>
{
    public override string Name => "divide";
    public override string Description => "Divide first number by second";

    protected override double Handle(DivideParams p)
    {
        if (p.B == 0)
            throw new ArgumentException("Cannot divide by zero");
        return p.A / p.B;
    }
}
