using FluentValidation;

namespace Application.Features.Media.Update.Commands;

public class UpdateMediaValidator : AbstractValidator<UpdateMediaCommand>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public UpdateMediaValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required")
            .Must(IsImage)
            .WithMessage("Only image files are allowed");

        RuleFor(x => x.Data)
            .NotNull()
            .WithMessage("File content is required")
            .Must(d => d is { Length: > 0 })
            .WithMessage("File content must not be empty");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0")
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)} MB");
    }

    private static bool IsImage(string contentType) =>
        !string.IsNullOrWhiteSpace(contentType)
        && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
