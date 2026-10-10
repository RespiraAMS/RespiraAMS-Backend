namespace Respira.Clinical.Domain.Models
{
    public record Evidence
    {
        /// <summary>
        /// The source that this evidence comes from (for example, a metric name)
        /// </summary>
        public string Source { get; init; }

        /// <summary>
        /// The string represetation of the formula used to evaludate criterion
        /// </summary>
        public string Formula { get; init; }

        /// <summary>
        /// The observation of clinical variables used to evaluate this criterion
        /// </summary>
        public List<ClinicalObservation> RelatedObservations { get; init; }

        /// <summary>
        /// The clinical variables that are missing in the observations
        /// which are  for this criterion evaluation
        /// </summary>
        public List<VariableRef> MissingVariables { get; init; }

        /// <summary>
        /// Evaluated value from the criterion formula
        /// </summary>
        public object? EvaluatedValue { get; init; }

        /// <summary>
        /// Score value, if this is scoring rule
        /// </summary>
        public decimal? Score { get; init; }

        /// <summary>
        /// Boolean flag, indiciate if this is a major criteria,
        /// used in major/minor metric system
        /// </summary>
        public bool? IsMajor { get; init; }

        public Evidence(string source, Formula formula, List<VariableRef> variablesUsed, List<ClinicalObservation> observations)
        {
            Source = source;
            Formula = formula.ToString()!;

            // Filter related observations from the list of all observations and used variables
            RelatedObservations = [.. observations.Where(x => variablesUsed.Select(x => x.Code).Contains(x.Variable.Code))];

            // Filter missing variables from the list of all observations and used variables
            MissingVariables = [.. variablesUsed.Where(x => !RelatedObservations.Any(ro => ro.Variable.Code.Equals(x.Code)))];

            // Evaluated value
            if (MissingVariables.Count == 0)
            {
                EvaluatedValue = formula.ToExpression(observations).Evaluate();
            }
        }

        public Evidence(string source, Formula formula, List<VariableRef> variablesUsed, List<ClinicalObservation> observations, decimal score) : this(source, formula, variablesUsed, observations)
        {
            Score = score;
        }

        public Evidence(string source, Formula formula, List<VariableRef> variablesUsed, List<ClinicalObservation> observations, bool isMajor) : this(source, formula, variablesUsed, observations)
        {
            IsMajor = isMajor;
        }

    }
}
