namespace Media.Application.Constracts.Storage
{
    public sealed record UploadRequest
    {
        /// <summary>Original file name.</summary>
        public required string FileName { get; init; }

        /// <summary>Stream containing the file content.</summary>
        public required Stream Content { get; init; }

        /// <summary>MIME content type.</summary>
        public required string ContentType { get; init; }

        /// <summary>Optional folder/prefix inside the bucket.</summary>
        public string? Folder { get; init; }
    }
}
