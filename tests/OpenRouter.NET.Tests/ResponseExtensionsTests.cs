using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tests;

public class ResponseExtensionsTests
{
    [Fact]
    public void GetContent_ShouldReturnFirstChoiceContent()
    {
        var response = new ChatCompletionResponse
        {
            Choices = new List<Choice>
            {
                new Choice
                {
                    Message = new Message { Content = "Hello, world!" }
                }
            }
        };
        
        var content = response.GetContent();
        
        Assert.Equal("Hello, world!", content);
    }
    
    [Fact]
    public void GetContent_WhenNoChoices_ShouldReturnNull()
    {
        var response = new ChatCompletionResponse
        {
            Choices = new List<Choice>()
        };
        
        var content = response.GetContent();
        
        Assert.Null(content);
    }
    
    [Fact]
    public void GetToolCalls_ShouldReturnToolCallsFromMessage()
    {
        var response = new ChatCompletionResponse
        {
            Choices = new List<Choice>
            {
                new Choice
                {
                    Message = new Message 
                    { 
                        ToolCalls = new ToolCall[]
                        {
                            new ToolCall
                            {
                                Id = "call_123",
                                Function = new FunctionCall
                                {
                                    Name = "get_weather",
                                    Arguments = "{\"location\":\"London\"}"
                                }
                            }
                        }
                    }
                }
            }
        };
        
        var toolCalls = response.GetToolCalls();
        
        Assert.NotNull(toolCalls);
        Assert.Single(toolCalls);
        Assert.Equal("call_123", toolCalls[0].Id);
        Assert.Equal("get_weather", toolCalls[0].Function.Name);
    }
    
    [Fact]
    public void GetFinishReason_ShouldReturnFirstChoiceFinishReason()
    {
        var response = new ChatCompletionResponse
        {
            Choices = new List<Choice>
            {
                new Choice
                {
                    FinishReason = "stop"
                }
            }
        };
        
        var finishReason = response.GetFinishReason();
        
        Assert.Equal("stop", finishReason);
    }
}

