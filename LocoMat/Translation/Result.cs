namespace LocoMat.Translation;

public class Result<T>
{
    public T Value { get; }
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    private Result(T value, bool success, string errorMessage)
    {
        Value = value;
        IsSuccess = success;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, null);
    }

    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>(default, false, errorMessage);
    }

    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}
