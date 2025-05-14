using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Web.Requests;
using Web.Services;

namespace Web.Controllers;

[ApiController]
[Route("[controller]")]
public class AudioController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _openAiOptions;
    private readonly AudioService _audioService;
    
    
    public AudioController(ILogger<AudioController> logger,
        IConfiguration configuration,
        IOptions<OpenAiOptions> openAi, AudioService audioService)
    {
        _audioService = audioService;
        _httpClient = new HttpClient();
        _openAiOptions = openAi.Value;
    }
    
    [HttpGet]
    public IActionResult GetKeys()
    {
        var apiKey = _openAiOptions.ApiKey;
        var voice = _openAiOptions.Voice;
        var model = _openAiOptions.Model;
        var path = _openAiOptions.AudioOutputPath;
        return Ok(new { apiKey, voice });
    }

    [HttpPost("speak")]
    public async Task<IActionResult> Speak([FromBody] TextRequest request)
    {
        var apiKey = _openAiOptions.ApiKey;
        var voice = _openAiOptions.Voice;
        var model = _openAiOptions.Model;
        var outputPath = _openAiOptions.AudioOutputPath;

        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest("API key is not configured.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            voice,
            input = request.Text
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var audioBytes = await response.Content.ReadAsByteArrayAsync();

        // Save to local file system
        await System.IO.File.WriteAllBytesAsync(outputPath, audioBytes);

        return Ok(new { message = "Audio saved", path = outputPath });
    }

    [HttpPost("speak-with-service")]
    public async Task<IActionResult> SpeakWithService(int rackNumber)
    {
        var numberString = rackNumber.ToString();
        var result = await _audioService.Execute(numberString);
        if(!result.IsSuccess)
            return StatusCode((int)result.HttpResponse.StatusCode, await result.HttpResponse.Content.ReadAsStringAsync());
        
        return Ok(result.Path);
    }
}