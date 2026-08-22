using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
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
