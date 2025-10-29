namespace OpenRouter.NET;

public class OpenRouterException : Exception
{
    public int? StatusCode { get; }
    public string? ErrorCode { get; }
    
    public OpenRouterException(string message) : base(message)
    {
    }
    
    public OpenRouterException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public OpenRouterException(string message, int? statusCode, string? errorCode = null) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class OpenRouterAuthException : OpenRouterException
{
    public OpenRouterAuthException(string message) : base(message, 401, "authentication_error")
    {
    }
}

public class OpenRouterRateLimitException : OpenRouterException
{
    public int? RetryAfterSeconds { get; }
    
    public OpenRouterRateLimitException(string message, int? retryAfterSeconds = null) 
        : base(message, 429, "rate_limit_exceeded")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public class OpenRouterModelNotFoundException : OpenRouterException
{
    public string ModelId { get; }
    
    public OpenRouterModelNotFoundException(string modelId, string message) 
        : base(message, 404, "model_not_found")
    {
        ModelId = modelId;
    }
}

public class OpenRouterBadRequestException : OpenRouterException
{
    public OpenRouterBadRequestException(string message) : base(message, 400, "bad_request")
    {
    }
}

public class OpenRouterServerException : OpenRouterException
{
    public OpenRouterServerException(string message) : base(message, 500, "server_error")
    {
    }
}

