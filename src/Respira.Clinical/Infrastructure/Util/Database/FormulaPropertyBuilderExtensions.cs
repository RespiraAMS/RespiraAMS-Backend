using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Respira.Clinical.Domain.Models;

namespace Respira.Clinical.Infrastructure.Util.Database
{
    /// <summary>
    /// Shared jsonb mapping for <see cref="Formula"/> properties, so every owning entity
    /// (Criterion.Formula, ScoringRule.ScoreFunction, ...) maps them exactly the same way
    /// without duplicating the converter/comparer registration.
    /// </summary>
    public static class FormulaPropertyBuilderExtensions
    {
        // Formula trees are immutable reference graphs; EF can only detect changes through
        // structural equality, so compare (and hash) the serialized jsonb payloads.
        private static readonly ValueComparer<Formula> s_formulaComparer = new(
            (l, r) => (l == null && r == null) || (l != null && r != null && FormulaSerializer.Serialize(l) == FormulaSerializer.Serialize(r)),
            v => v == null ? 0 : FormulaSerializer.Serialize(v).GetHashCode(),
            v => v);

        public static PropertyBuilder<Formula> HasFormulaConversion(this PropertyBuilder<Formula> property)
        {
            property
                .HasColumnType("jsonb")
                .HasConversion(
                    v => FormulaSerializer.Serialize(v),
                    v => FormulaSerializer.Deserialize(v))
                .Metadata.SetValueComparer(s_formulaComparer);

            return property;
        }
    }
}
