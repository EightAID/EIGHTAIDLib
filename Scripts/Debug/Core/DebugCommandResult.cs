#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
public readonly struct DebugCommandResult
{
    public bool IsSuccess { get; }
    public string Message { get; }

    private DebugCommandResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message ?? string.Empty;
    }

    public static DebugCommandResult Success(string message = "")
    {
        return new DebugCommandResult(true, message);
    }

    public static DebugCommandResult Failure(string message)
    {
        return new DebugCommandResult(false, message);
    }
}
#endif
