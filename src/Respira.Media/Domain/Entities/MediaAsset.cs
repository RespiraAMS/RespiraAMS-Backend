using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// Represents an uploaded media file (image, document, etc.) stored in object storage.
    /// </summary>
    public class MediaAsset : Base
    {
        public required string FileName { get; set; }
        public string? Url { get; set; }
        public string? ObjectKey { get; set; }
        public string? BucketName { get; set; }
        public string? ContentType { get; set; }
        public long? Size { get; set; }
    }
}
