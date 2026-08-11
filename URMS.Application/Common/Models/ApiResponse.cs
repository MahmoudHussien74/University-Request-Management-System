namespace URMS.Application.Common.Models;

public class ApiError
{
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;

    public ApiError() { }

    public ApiError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = default!;
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Operation completed successfully.", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Message = message,
            Data = data,
            Errors = null
        };
    }

    public static ApiResponse<T> Failure(string message, List<ApiError> errors, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Message = message,
            Data = default,
            Errors = errors
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Success(string message = "Operation completed successfully.", int statusCode = 200)
    {
        return new ApiResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Message = message,
            Data = null,
            Errors = null
        };
    }

    public static new ApiResponse Failure(string message, List<ApiError> errors, int statusCode = 400)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Message = message,
            Data = null,
            Errors = errors
        };
    }
}
