namespace Media.Application.Validators
{
    /// <summary>
    /// Validates that an uploaded file is an image. The MIME content type is derived
    /// from the actual file content (magic bytes sniffing) instead of trusting the
    /// caller, and the file name (when provided) must carry an allowed image extension.
    /// </summary>
    public static class ImageFileValidator
    {
        /// <summary>Number of leading bytes read for signature sniffing.</summary>
        private const int HeaderLength = 32;

        private static readonly byte[] PngSignature =
        [
            0x89,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A,
        ];

        /// <summary>Image MIME content types that can be detected.</summary>
        public static readonly IReadOnlyCollection<string> AllowedContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/avif",
            "image/bmp",
            "image/tiff",
        ];

        /// <summary>Image file extensions accepted when a file name is provided.</summary>
        public static readonly IReadOnlyCollection<string> AllowedExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".avif",
            ".bmp",
            ".tif",
            ".tiff",
        ];

        /// <summary>Human readable list of supported image content types (for error messages).</summary>
        public static string AllowedContentTypesText => string.Join(", ", AllowedContentTypes);

        /// <summary>Human readable list of accepted extensions (for error messages).</summary>
        public static string AllowedExtensionsText => string.Join(", ", AllowedExtensions);

        /// <summary>
        /// Reads the leading bytes of the stream and detects the image MIME content type
        /// from the file signature. Returns null when the content is not a supported image.
        /// The stream must be readable and seekable; its position is restored afterwards.
        /// </summary>
        public static async Task<string?> DetectContentTypeAsync(
            Stream stream,
            CancellationToken cancellationToken = default
        )
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                return null;
            }

            var originalPosition = stream.Position;
            stream.Position = 0;

            var header = new byte[HeaderLength];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = await stream.ReadAsync(
                    header.AsMemory(bytesRead, header.Length - bytesRead),
                    cancellationToken
                );

                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            stream.Position = originalPosition;

            return DetectContentType(header.AsSpan(0, bytesRead));
        }

        /// <summary>Returns true when the file name has an allowed image extension.</summary>
        public static bool IsAllowedExtension(string? fileName)
        {
            var extension = Path.GetExtension(fileName ?? string.Empty);

            return !string.IsNullOrEmpty(extension)
                && AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Maps an allowed image content type to its canonical file extension.</summary>
        public static string GetExtension(string contentType)
        {
            return contentType.Trim().ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/avif" => ".avif",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tif",
                _ => string.Empty,
            };
        }

        private static string? DetectContentType(ReadOnlySpan<byte> header)
        {
            // JPEG: FF D8 FF
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return "image/jpeg";
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (
                header.Length >= PngSignature.Length
                && header[..PngSignature.Length].SequenceEqual(PngSignature)
            )
            {
                return "image/png";
            }

            // GIF: "GIF8" (covers GIF87a and GIF89a)
            if (header.Length >= 4 && header[..4].SequenceEqual("GIF8"u8))
            {
                return "image/gif";
            }

            // WebP: "RIFF" .... "WEBP"
            if (
                header.Length >= 12
                && header[..4].SequenceEqual("RIFF"u8)
                && header[8..12].SequenceEqual("WEBP"u8)
            )
            {
                return "image/webp";
            }

            // BMP: "BM"
            if (header.Length >= 2 && header[..2].SequenceEqual("BM"u8))
            {
                return "image/bmp";
            }

            // TIFF: little endian "II*\0" or big endian "MM\0*"
            if (
                header.Length >= 4
                && (
                    (
                        header[0] == 0x49
                        && header[1] == 0x49
                        && header[2] == 0x2A
                        && header[3] == 0x00
                    )
                    || (
                        header[0] == 0x4D
                        && header[1] == 0x4D
                        && header[2] == 0x00
                        && header[3] == 0x2A
                    )
                )
            )
            {
                return "image/tiff";
            }

            // AVIF: ISO BMFF container, bytes 4..8 = "ftyp" with an avif brand
            if (header.Length >= 12 && header[4..8].SequenceEqual("ftyp"u8))
            {
                for (var offset = 8; offset + 4 <= header.Length; offset += 4)
                {
                    var brand = header.Slice(offset, 4);

                    if (brand.SequenceEqual("avif"u8) || brand.SequenceEqual("avis"u8))
                    {
                        return "image/avif";
                    }
                }
            }

            return null;
        }
    }
}
