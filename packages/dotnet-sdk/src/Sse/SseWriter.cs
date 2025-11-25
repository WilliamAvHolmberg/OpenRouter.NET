using System.Text;
using System.Text.Json;

namespace OpenRouter.NET.Sse;

public class SseWriter
{
    private readonly Stream _stream;
    private readonly JsonSerializerOptions _jsonOptions;

    public SseWriter(Stream stream, JsonSerializerOptions? jsonOptions = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task WriteEventAsync(SseEvent sseEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(sseEvent, sseEvent.GetType(), _jsonOptions);
        await WriteDataAsync(json, cancellationToken);
    }

    public async Task WriteDataAsync(string data, CancellationToken cancellationToken = default)
    {
        var message = $"data: {data}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(bytes, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    public async Task WriteCommentAsync(string comment, CancellationToken cancellationToken = default)
    {
        var message = $": {comment}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(bytes, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    public static void SetupSseHeaders(Microsoft.AspNetCore.Http.HttpResponse response)
    {
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
    }
}
