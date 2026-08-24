namespace Application.Contracts.Services
{
    public record HeaderStyleOption(bool Align = false, bool Bold = false);

    public record Column(string Header, List<string> Values);

    public interface IExportService
    {
        Task<byte[]> Export(List<Column> columns, HeaderStyleOption? headerStyle = null);
    }
}
