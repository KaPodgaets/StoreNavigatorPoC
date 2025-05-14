namespace Web;

public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "tts-1";
    public string Voice { get; set; } = "nova";
    public string AudioOutputPath { get; set; } = string.Empty;
    public string ImageDirectoryPath { get; set; } = string.Empty;
    public string OutputJsonDirectoryPath { get; set; } = string.Empty;
    
}