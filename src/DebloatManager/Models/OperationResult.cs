namespace DebloatManager.Models;

public sealed class OperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool RestartRequired { get; init; }
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    public static OperationResult Ok(string message, bool restartRequired = false, IReadOnlyList<string>? issues = null)
        => new() { Success = true, Message = message, RestartRequired = restartRequired, Issues = issues ?? Array.Empty<string>() };

    public static OperationResult Fail(string message) => new() { Success = false, Message = message };
}
