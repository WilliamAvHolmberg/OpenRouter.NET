using System.Text.Json.Serialization;
using OpenRouter.NET.Models;
using OpenRouter.NET.Tools;

namespace StreamingWebApiSample;

// Typed version
public class SetOrderFiltersTool : VoidTool<OrderFilterArgs>
{
    public override string Name => "set_order_filters";
    public override string Description => "Apply filters to the order list in the client UI";
    public override ToolMode Mode => ToolMode.ClientSide;

    protected override void HandleVoid(OrderFilterArgs filters)
    {
        // Client-side tool: emitted to client, not executed here
    }
}

// Legacy method-based version (kept for reference)
public class OrderClientTools
{
    [ToolMethod("Apply filters to the order list in the client UI")]
    public string SetOrderFilters(
        [ToolParameter("Filter configuration to apply in the client")] OrderFilterArgs filters)
    {
        // Client-side tool: this method body will not execute when registered with ToolMode.ClientSide.
        // Returning a simple acknowledgement for completeness if executed in other modes.
        return "ok";
    }
}

public class OrderFilterArgs
{
    [JsonPropertyName("status")] public List<string>? Status { get; set; }
    [JsonPropertyName("delivered")] public bool? Delivered { get; set; }
    [JsonPropertyName("customerIds")] public List<string>? CustomerIds { get; set; }
    [JsonPropertyName("minAmount")] public decimal? MinAmount { get; set; }
    [JsonPropertyName("maxAmount")] public decimal? MaxAmount { get; set; }
    [JsonPropertyName("createdFrom")] public string? CreatedFrom { get; set; }
    [JsonPropertyName("createdTo")] public string? CreatedTo { get; set; }
    [JsonPropertyName("deliveredFrom")] public string? DeliveredFrom { get; set; }
    [JsonPropertyName("deliveredTo")] public string? DeliveredTo { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
}


