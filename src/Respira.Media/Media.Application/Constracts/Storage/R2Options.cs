namespace Media.Application.Constracts.Storage;

public sealed class R2Options
{
    public const string SectionName = "R2";
    public required string Endpoint { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string BucketName { get; init; }
    public required string PublicUrl { get; init; }
}
