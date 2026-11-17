using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Respira.Clinical.Domain.Models;

namespace Respira.Clinical.Infrastructure.Util.Database
{
    /// <summary>
    /// Shared jsonb mapping for <see cref="Formula"/> properties, so every owning entity
    /// (Criterion.Formula, ScoringRule.ScoreFunction, ClinicalVariable.Prerequisite, ...)
    /// maps them exactly the same way without duplicating the converter/comparer registration.
    /// </summary>
    public static class FormulaPropertyBuilderExtensions
    {
        // Formula trees are immutable reference graphs; EF can only detect changes through
        // structural equality, so compare (and hash) the serialized jsonb payloads.
        private static ValueComparer<TProperty> CreateFormulaComparer<TProperty>()
            where TProperty : Formula?
        {
            return new ValueComparer<TProperty>(
                (l, r) => (l == null && r == null) || (l != null && r != null && FormulaSerializer.Serialize(l) == FormulaSerializer.Serialize(r)),
                v => v == null ? 0 : FormulaSerializer.Serialize(v).GetHashCode(),
                v => v);
        }

        public static PropertyBuilder<TProperty> HasFormulaConversion<TProperty>(this PropertyBuilder<TProperty> property)
            where TProperty : Formula?
        {
            // Null model/provider values (e.g. ClinicalVariable.Prerequisite) propagate as null.
            Expression<Func<TProperty, string>> toProvider =
                v => v == null ? null! : FormulaSerializer.Serialize(v);
            Expression<Func<string, TProperty>> fromProvider =
                v => v == null ? default! : (TProperty)(object)FormulaSerializer.Deserialize(v);

            property
                .HasColumnType("jsonb")
                .HasConversion(toProvider, fromProvider)
                .Metadata.SetValueComparer(CreateFormulaComparer<TProperty>());

            return property;
        }
    }
}
