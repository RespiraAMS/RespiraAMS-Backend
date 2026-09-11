using Respira.Domain.Entities;

namespace Respira.Domain.Services
{
    /// <summary>
    /// Clinical context. This class contains all information required to perform
    /// clinical diagnosis
    /// </summary>
    /// <param name="variables">Clinical variables</param>
    /// <param name="metrics">Score metrics</param>
    public class ClinicalContext(IEnumerable<ClinicalVariable> variables, IEnumerable<ScoreMetrics> metrics)
    {
        public IEnumerable<ClinicalVariable> Variables { get; set; } = variables;
        public IEnumerable<ScoreMetrics> Metrics { get; set; } = metrics;
    }
}
