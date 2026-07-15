using FluentValidation;
using Respira.ServiceDefaults.Dtos;

namespace Respira.ServiceDefaults.Extensions;

/// <summary>
/// Custom Fluent validation rules that are used across microservices
/// </summary>
public static class CustomValidationRules
{
    /// <summary>
    /// Validate if pagination query parameters are valid.
    /// Valid parameters must have page index &gt;= 1 and 0 &lt; page size &lt;= 100   
    /// </summary>
    /// <param name="ruleBuilder">Fluent rule builder object</param>
    /// <typeparam name="T">Data type of the pagination's item</typeparam>
    /// <returns>Return Fluent IRuleBuilder (for continue chaining validation)</returns>
    public static IRuleBuilderOptions<T, PaginationParam> IsValidPaginationParam<T>(this IRuleBuilder<T, PaginationParam> ruleBuilder)
    {
        return ruleBuilder
            .Must(p => p.Page > 0)
            .WithMessage("Pagination page must be a positive integer")
            .Must(p => p.Size is > 0 and <= 100)
            .WithMessage("Pagination size must be a positive integer and less than or equal to 100");
    }
}