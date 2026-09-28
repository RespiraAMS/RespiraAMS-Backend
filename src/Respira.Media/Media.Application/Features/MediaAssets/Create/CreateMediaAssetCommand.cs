using Respira.ServiceDefaults.Contracts.CQRS;

namespace Media.Application.Features.MediaAssets.Create
{
    public record CreateMediaAssetCommand : ICommand
    {
        public required Stream MediaFile { get; init; }

        /// <summary>
        /// Optional original file name. When omitted, a unique name is generated
        /// from the detected content type.
        /// </summary>
        public string? FileName { get; init; }

        /// <summary>
        /// Optional content type hint. The content type detected from the actual
        /// file content always takes precedence.
        /// </summary>
        public string? ContentType { get; init; }

        /// <summary>Optional folder/prefix inside the R2 bucket.</summary>
        public string? Folder { get; init; }
    }
}
