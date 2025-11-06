using OpenRouter.NET;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;
using Xunit;

namespace OpenRouter.NET.Tests;

public class TypedToolTests
{
    // Test parameter and result classes
    public class AddParams
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    public class AddTool : Tool<AddParams, int>
    {
        public override string Name => "add";
        public override string Description => "Add two numbers";

        protected override int Handle(AddParams parameters)
        {
            return parameters.A + parameters.B;
        }
    }

    public class GreetParams
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GreetResult
    {
        public string Greeting { get; set; } = string.Empty;
        public int Length { get; set; }
    }

    public class GreetTool : Tool<GreetParams, GreetResult>
    {
        public override string Name => "greet";
        public override string Description => "Generate a greeting";

        protected override GreetResult Handle(GreetParams parameters)
        {
            var greeting = $"Hello, {parameters.Name}!";
            return new GreetResult
            {
                Greeting = greeting,
                Length = greeting.Length
            };
        }
    }

    public class LogParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class LogTool : VoidTool<LogParams>
    {
        public override string Name => "log";
        public override string Description => "Log a message";
        public override ToolMode Mode => ToolMode.ClientSide;

        public string? LastMessage { get; private set; }

        protected override void HandleVoid(LogParams parameters)
        {
            LastMessage = parameters.Message;
        }
    }

    [Fact]
    public void TypedTool_WithPrimitiveResult_ExecutesCorrectly()
    {
        var tool = new AddTool();

        var result = tool.Execute(new AddParams { A = 5, B = 3 });

        Assert.Equal(8, result);
    }

    [Fact]
    public void TypedTool_WithComplexResult_ExecutesCorrectly()
    {
        var tool = new GreetTool();

        var result = tool.Execute(new GreetParams { Name = "World" });

        Assert.Equal("Hello, World!", result.Greeting);
        Assert.Equal(13, result.Length);
    }

    [Fact]
    public void VoidTool_ExecutesCorrectly()
    {
        var tool = new LogTool();

        tool.Execute(new LogParams { Message = "Test message" });

        Assert.Equal("Test message", tool.LastMessage);
    }

    [Fact]
    public void TypedTool_PropertiesAreAccessible()
    {
        var tool = new AddTool();

        Assert.Equal("add", tool.Name);
        Assert.Equal("Add two numbers", tool.Description);
        Assert.Equal(ToolMode.AutoExecute, tool.Mode);
    }

    [Fact]
    public void VoidTool_HasClientSideMode()
    {
        var tool = new LogTool();

        Assert.Equal(ToolMode.ClientSide, tool.Mode);
    }

    [Fact]
    public void TypedTool_Registration_DoesNotThrow()
    {
        var client = new OpenRouterClient("test-key");

        // Should not throw
        var exception = Record.Exception(() =>
        {
            client.RegisterTool<AddTool>();
            client.RegisterTool<GreetTool>();
            client.RegisterTool<LogTool>();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void TypedTool_Registration_AddsToolsToClient()
    {
        var client = new OpenRouterClient("test-key");

        client.RegisterTool<AddTool>();
        client.RegisterTool<GreetTool>();

        var tools = client.GetTools();

        Assert.Contains(tools, t => t.Function.Name == "add");
        Assert.Contains(tools, t => t.Function.Name == "greet");
    }
}
