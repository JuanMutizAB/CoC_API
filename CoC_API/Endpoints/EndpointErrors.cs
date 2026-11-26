namespace CoC_API.Endpoints;

internal static class EndpointErrors
{
    public static string FormatErrorMessage(Exception ex)
    {
        var message = ex.Message;
        if (ex is ArgumentException { ParamName.Length: > 0 } argEx)
        {
            var suffix = $" (Parameter '{argEx.ParamName}')";
            if (message.EndsWith(suffix, StringComparison.Ordinal))
                return message[..^suffix.Length];
        }

        return message;
    }
}
