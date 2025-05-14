namespace Web.Services;

public class AudioServiceResult
{
    public AudioServiceResult(bool isSuccess, HttpResponseMessage response, string? path)
    {
        IsSuccess = isSuccess;
        HttpResponse = response;
        Path = path;
    }
    public bool IsSuccess { get; set; } = false;

    public HttpResponseMessage HttpResponse { get; set; } 
    public string? Path { get; set; }
}