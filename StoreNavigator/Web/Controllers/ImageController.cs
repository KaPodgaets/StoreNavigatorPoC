using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Web.Requests;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly OpenAiOptions _options;
    private readonly HttpClient _httpClient;

    public ImageController(IOptions<OpenAiOptions> options)
    {
        _options = options.Value;
        _httpClient = new HttpClient();
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeImage([FromBody] PhotoAnalyzeRequest photoAnalyzeRequest)
    {
        // Validation of camera number
        if (photoAnalyzeRequest.CameraNumber < 1 && photoAnalyzeRequest.CameraNumber > 3)
            return BadRequest();

        // create file pathes
        var filePath = Path.Combine(_options.ImageDirectoryPath, "storage" + photoAnalyzeRequest.CameraNumber + ".jpg");
        var jsonPath = Path.Combine(_options.OutputJsonDirectoryPath, "rack" + photoAnalyzeRequest.CameraNumber + "_output.json");

        // check file exists
        if (!System.IO.File.Exists(filePath))
            return NotFound("Image file not found.");

        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        string base64Image = Convert.ToBase64String(imageBytes);

        var request = new
        {
            model = "gpt-4o",
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text =
                                $"describe all product categories and subcategories. response with json. add information to json that this is rack number {photoAnalyzeRequest.CameraNumber} in store"
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:image/jpeg;base64,{base64Image}"
                            }
                        }
                    }
                }
            },
            max_tokens = 1000
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var resultJson = await response.Content.ReadAsStringAsync();

        // Optional: format response
        using var doc = JsonDocument.Parse(resultJson);
        var contentText = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (contentText == null)
            return StatusCode(500, "No content returned");

        await System.IO.File.WriteAllTextAsync(jsonPath, contentText);

        return Ok(new { saved = jsonPath });
    }
}