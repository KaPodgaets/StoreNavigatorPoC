using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Web.Requests;
using Web.Services;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionController : ControllerBase
{
    private readonly OpenAiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly QuestionService _questionService;
    private readonly AudioService _audioService;

    public QuestionController(
        IOptions<OpenAiOptions> options, 
        QuestionService questionService,
        AudioService audioService)
    {
        _questionService = questionService;
        _audioService = audioService;
        _options = options.Value;
        _httpClient = new HttpClient();
    }
    
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var combinedJsons = new StringBuilder();
        var listRacks = new List<string> {"1", "2", "3"};
        
        foreach (var rackNumber in listRacks)
        {
            var path = Path.Combine(_options.OutputJsonDirectoryPath, "rack" + rackNumber + "_output.json");
            if (!System.IO.File.Exists(path)) continue;
            combinedJsons.AppendLine(await System.IO.File.ReadAllTextAsync(path));
        }

        var prompt = $"""
        act as store assistant and guide. 
        There is the list of categories and subcategories of products on supermarket racks in few json. 
        please use only categories and subcategories, ignore the brands.
        I will provide a question about where located some products in the store.
        You have to give me as an answer only the store number. 
        If you do not understand the question answer "-1", 
        or if there is no such product category in data, just answer "0".

        #racks
        {combinedJsons}

        #question
        {request.Question}
        """;

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 100
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var resultJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(resultJson);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Ok(new { response = message?.Trim() ?? "-1" });
    }
    
    [HttpPost("responseWithAudio")]
    public async Task<IActionResult> ResponseWithAudio([FromBody] QuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var questionServiceResult = await _questionService.GetRackNumber(request.Question);
        if (!questionServiceResult.IsSuccess)
            return StatusCode((int)questionServiceResult.HttpResponse.StatusCode, await questionServiceResult.HttpResponse.Content.ReadAsStringAsync());
        
        if(questionServiceResult.RackNumber is null)
            return BadRequest("Rack number is invalid.");
        
        var audioServiceResult = await _audioService.Execute(questionServiceResult.RackNumber);
        if(!audioServiceResult.IsSuccess)
            return StatusCode((int)audioServiceResult.HttpResponse.StatusCode, await audioServiceResult.HttpResponse.Content.ReadAsStringAsync());
        
        
        return Ok(audioServiceResult.Path);
    }
    
    [HttpPost("responseWithAudioFile")]
    public async Task<IActionResult> ResponseWithAudioFile([FromBody] QuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var questionServiceResult = await _questionService.GetRackNumber(request.Question);
        if (!questionServiceResult.IsSuccess)
            return StatusCode((int)questionServiceResult.HttpResponse.StatusCode, await questionServiceResult.HttpResponse.Content.ReadAsStringAsync());
        
        if(questionServiceResult.RackNumber is null)
            return BadRequest("Rack number is invalid.");
        
        var audioServiceResult = await _audioService.Execute(questionServiceResult.RackNumber);
        if(!audioServiceResult.IsSuccess)
            return StatusCode((int)audioServiceResult.HttpResponse.StatusCode, await audioServiceResult.HttpResponse.Content.ReadAsStringAsync());
        
        
        return Ok(audioServiceResult.Path);
    }
}