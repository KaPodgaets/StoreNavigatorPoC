namespace Web.Services;

public class QuestionServiceResult
{
    public QuestionServiceResult(bool isSuccess, HttpResponseMessage response, string? message)
    {
        IsSuccess = isSuccess;
        HttpResponse = response;
        RackNumber = message;
    }
    public bool IsSuccess { get; set; } = false;

    public HttpResponseMessage HttpResponse { get; set; } 
    public string? RackNumber { get; set; }
}