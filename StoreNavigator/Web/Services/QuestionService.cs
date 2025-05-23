﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Web.Services;

public class QuestionService
{
    private readonly OpenAiOptions _options;
    private readonly HttpClient _httpClient;

    public QuestionService(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    
    public async Task<QuestionServiceResult> GetRackNumber(string question)
    {
        var combinedJsons = new StringBuilder();
        var listRacks = new List<string> { "1", "2", "3" };

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
                      {question}
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
            return new QuestionServiceResult(false, response, null);

        var resultJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(resultJson);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        
        return new QuestionServiceResult(true, response, message);
    }
}