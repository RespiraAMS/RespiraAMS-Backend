using NpgsqlTypes;

namespace Respira.ServiceDefaults.Models
{
    public class FullTextSearch : Base
    {
        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }
}
