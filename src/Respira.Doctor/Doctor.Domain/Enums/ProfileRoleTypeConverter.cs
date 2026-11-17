using System.ComponentModel;
using System.Globalization;

namespace Respira.Doctor.Domain.Enums
{
    public class ProfileRoleTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object value
        )
        {
            if (value is string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    return null;
                }

                var cleaned = str.Replace("-", "").Replace("_", "").Trim();

                if (cleaned.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                {
                    return ProfileRole.Doctor;
                }

                if (
                    cleaned.Equals("HeadDepartmentDoctor", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("HeadDoctor", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("HeadOfDepartment", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("HeadDepartment", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return ProfileRole.HeadDepartmentDoctor;
                }

                throw new FormatException(
                    $"Unknown profile role '{str}'. Expected 'Doctor' or 'HeadDepartmentDoctor'."
                );
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
