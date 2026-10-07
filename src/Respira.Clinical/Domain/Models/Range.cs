namespace Respira.Clinical.Domain.Models
{
    public class Range
    {
        public required decimal Min { get; set; }
        public required bool IsMinExclusive { get; set; }
        public required decimal Max { get; set; }
        public required bool IsMaxExclusive { get; set; }
        public required string? Unit { get; set; }

        public bool IsInRange(decimal value)
        {
            var result = Min < value && value < Max;
            if (!IsMinExclusive)
            {
                result = result || value == Min;
            }
            if (!IsMaxExclusive)
            {
                result = result || value == Max;
            }
            return result;
        }

        public bool IsRangeOverlapped(Range? range)
        {
            if (range is null) return false;

            // This range is completely to the left of the other range.
            if (Max < range.Min) return false;
            if (Max == range.Min)
            {
                return !IsMaxExclusive && !range.IsMinExclusive;
            }

            // This range is completely to the right of the other range.
            if (Min > range.Max) return false;
            if (Min == range.Max)
            {
                return !IsMinExclusive && !range.IsMaxExclusive;
            }

            return true;
        }

        /// <summary>
        /// Check if the current range is contained within the other range.
        /// Basically, all element in this range must be in the parameter range.
        /// </summary>
        /// <param name="range">Container range</param>
        /// <returns>True if is contained</returns>
        public bool IsRangeContained(Range? range)
        {
            if (range is null) return false;

            var lower = range.Min < Min || (range.Min == Min && (!range.IsMinExclusive || IsMinExclusive));
            var upper = range.Max > Max || (range.Max == Max && (!range.IsMaxExclusive || IsMaxExclusive));
            return lower && upper;
        }

        public override string ToString()
        {
            if (Max == decimal.MaxValue)
            {
                return $"{(IsMinExclusive ? ">" : "≥")} {Min}";
            }
            else
            {
                return $"{(IsMinExclusive ? ">" : "≥")} {Min} và {(IsMaxExclusive ? "<" : "≤")} {Max}";
            }

            // return $"{(IsMinExclusive ? "(" : "[")}{Min}, {(Max == decimal.MaxValue ? "∞" : Max)}{(IsMaxExclusive ? ")" : "]")}";
        }
    }
}
