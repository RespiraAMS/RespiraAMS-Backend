using Respira.Domain.Entities;

namespace Respira.Domain.Services
{
    public class ClinicalContext(IEnumerable<ClinicalVariable> variables, IEnumerable<ScoreMetrics> metrics)
    {
        public IEnumerable<ClinicalVariable> Variables { get; set; } = variables;
        public IEnumerable<ScoreMetrics> Metrics { get; set; } = metrics;
    }
}
