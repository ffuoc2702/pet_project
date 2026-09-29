namespace MatchingSports.Application.Common.Exceptions;

public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string code, string message)
    : AppException(code, message);

public sealed class ConflictException(string code, string message)
    : AppException(code, message);

public sealed class UnauthorizedException(string code, string message)
    : AppException(code, message);

public sealed class ForbiddenException(string code, string message)
    : AppException(code, message);

public sealed class ServiceUnavailableException(string code, string message)
    : AppException(code, message);

public sealed class RequestValidationException : AppException
{
    public RequestValidationException(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]> errors)
        : base(code, message)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
