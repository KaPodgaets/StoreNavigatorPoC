using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Web.Services;

public class AudioService
{
    private readonly OpenAiOptions _options;
    private readonly HttpClient _httpClient;

    public AudioService(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AudioServiceResult> Execute(string rackNumber)
    {
        const string prompt = "המוצר נמצא במעבר";
        var apiKey = _options.ApiKey;
        var voice = _options.Voice;
        var model = _options.Model;
        var outputPath = _options.AudioOutputPath;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            voice,
            input = prompt + $" {rackNumber}."
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

        if (!response.IsSuccessStatusCode)
            return new AudioServiceResult(false, response, null);

        var audioBytes = await response.Content.ReadAsByteArrayAsync();

        // Save to local file system
        await System.IO.File.WriteAllBytesAsync(outputPath, audioBytes);

        return new AudioServiceResult(true, response, outputPath);
    }
}