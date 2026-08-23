using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// Represents an uploaded media file (image, document, etc.) stored in object storage.
    /// </summary>
    public class MediaAsset : Base
    {
        /// <summary>Original file name of the uploaded asset.</summary>
        public required string FileName { get; set; }

        /// <summary>Accessible URL of the stored object, if available.</summary>
        public string? Url { get; set; }

        /// <summary>Key identifying the object within the storage bucket.</summary>
        public string? ObjectKey { get; set; }

        /// <summary>Name of the storage bucket holding the object.</summary>
        public string? BucketName { get; set; }

        /// <summary>MIME content type of the asset (e.g. image/png).</summary>
        public string? ContentType { get; set; }

        /// <summary>Size of the asset in bytes, if known.</summary>
        public long? Size { get; set; }
    }
}
