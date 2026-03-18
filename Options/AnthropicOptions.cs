namespace BookLibrary.Options;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public string ApiKey { get; init; } = string.Empty;
    public string Model  { get; init; } = "claude-3-5-haiku-20241022";
}
