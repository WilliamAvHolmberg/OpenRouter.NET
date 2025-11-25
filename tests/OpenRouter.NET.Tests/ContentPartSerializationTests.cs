using System.Text.Json;
using OpenRouter.NET.Models;

namespace OpenRouter.NET.Tests;

public class ContentPartSerializationTests
{
    [Fact]
    public void ContentPart_WithTextContent_ShouldSerializeTextProperty()
    {
        var textContent = new TextContent("Hello, world!");
        var contentList = new List<ContentPart> { textContent };

        var json = JsonSerializer.Serialize(contentList);

        Assert.Contains("\"text\"", json);
        Assert.Contains("Hello, world!", json);
        Assert.Contains("\"type\":\"text\"", json);
    }

    [Fact]
    public void ContentPart_WithImageContent_ShouldSerializeImageUrlProperty()
    {
        var imageContent = new ImageContent("data:image/png;base64,iVBORw0KGgoAAAANS");
        var contentList = new List<ContentPart> { imageContent };

        var json = JsonSerializer.Serialize(contentList);

        Assert.Contains("\"image_url\"", json);
        Assert.Contains("data:image/png;base64,iVBORw0KGgoAAAANS", json);
        Assert.Contains("\"type\":\"image_url\"", json);
    }

    [Fact]
    public void ContentPart_WithMixedContent_ShouldSerializeBothTextAndImage()
    {
        var contentList = new List<ContentPart>
        {
            new TextContent("Describe this image:"),
            new ImageContent("data:image/jpeg;base64,/9j/4AAQSkZJRg")
        };

        var json = JsonSerializer.Serialize(contentList);

        Assert.Contains("\"text\"", json);
        Assert.Contains("Describe this image:", json);
        Assert.Contains("\"image_url\"", json);
        Assert.Contains("data:image/jpeg;base64,/9j/4AAQSkZJRg", json);
        Assert.Contains("\"type\":\"text\"", json);
        Assert.Contains("\"type\":\"image_url\"", json);
    }

    [Fact]
    public void Message_WithContentPartList_ShouldSerializeCorrectly()
    {
        var message = Message.FromUser(new List<ContentPart>
        {
            new TextContent("What is in this image?"),
            new ImageContent("data:image/png;base64,abc123", "high")
        });

        var json = JsonSerializer.Serialize(message);

        Assert.Contains("\"role\":\"user\"", json);
        Assert.Contains("\"text\"", json);
        Assert.Contains("What is in this image?", json);
        Assert.Contains("\"image_url\"", json);
        Assert.Contains("data:image/png;base64,abc123", json);
        Assert.Contains("\"detail\":\"high\"", json);
    }

    [Fact]
    public void ImageUrl_ShouldSerializeAllProperties()
    {
        var imageContent = new ImageContent("https://example.com/image.png", "low");
        var json = JsonSerializer.Serialize(imageContent);

        Assert.Contains("\"url\":\"https://example.com/image.png\"", json);
        Assert.Contains("\"detail\":\"low\"", json);
    }

    [Fact]
    public void ContentPart_List_ShouldNotSerializeOnlyTypeProperty()
    {
        var contentList = new List<ContentPart>
        {
            new TextContent("Test text"),
            new ImageContent("data:image/png;base64,test")
        };

        var json = JsonSerializer.Serialize(contentList);

        var deserializedAsObjects = JsonSerializer.Deserialize<List<JsonElement>>(json);
        
        Assert.NotNull(deserializedAsObjects);
        Assert.Equal(2, deserializedAsObjects.Count);

        var firstElement = deserializedAsObjects[0];
        Assert.True(firstElement.TryGetProperty("type", out _), "First element should have 'type' property");
        Assert.True(firstElement.TryGetProperty("text", out _), "First element should have 'text' property (THIS WILL FAIL WITHOUT THE FIX)");

        var secondElement = deserializedAsObjects[1];
        Assert.True(secondElement.TryGetProperty("type", out _), "Second element should have 'type' property");
        Assert.True(secondElement.TryGetProperty("image_url", out _), "Second element should have 'image_url' property (THIS WILL FAIL WITHOUT THE FIX)");
    }
}
