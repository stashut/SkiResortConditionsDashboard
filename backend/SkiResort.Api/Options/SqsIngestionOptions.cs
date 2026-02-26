namespace SkiResort.Api.Options;

public sealed class SqsIngestionOptions
{
    public string QueueUrl { get; set; } = string.Empty;
    public int WaitTimeSeconds { get; set; } = 10;
    public int MaxMessages { get; set; } = 10;
}

