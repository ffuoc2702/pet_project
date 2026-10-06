namespace MatchingSports.Shared;

public static class ArgumentExceptionExtensions
{
    public static string GetValidationMessage(this ArgumentException exception)
    {
        if (exception.ParamName is null)
            return exception.Message;

        // The framework appends a localized parameter suffix; the response already identifies the field.
        var parameterSuffix = new ArgumentException(string.Empty, exception.ParamName).Message;
        return exception.Message.EndsWith(parameterSuffix, StringComparison.Ordinal)
            ? exception.Message[..^parameterSuffix.Length].TrimEnd()
            : exception.Message;
    }
}
