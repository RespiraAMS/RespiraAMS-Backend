using Microsoft.Extensions.Logging;
using Moq;
using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.Domain.Services;
using Respira.ServiceDefaults.Contracts.Results;
using Xunit;

namespace Respira.Domain.Test.Services
{
    public class DiagnoseServiceTest
    {
        private readonly IDiagnoseService _service;
        private static readonly ClinicalContext _context = CreateContext();

        public DiagnoseServiceTest()
        {
            var logger = new Mock<ILogger<DiagnoseService>>().Object;
            _service = new DiagnoseService(_context, logger);
        }

        private static ClinicalContext CreateContext()
        {
            List<ClinicalVariable> variables = [
                new ClinicalVariable
                {
                    Name = "Age",
                    Code = "AGE",
                    Description = "Patient age",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "year"
                },

                new ClinicalVariable
                {
                    Name = "Female sex",
                    Code = "FEMALE",
                    Description = "Is the patient female",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Height",
                    Code = "HEIGHT",
                    Description = "Patient height",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "cm"
                },

                new ClinicalVariable
                {
                    Name = "Weight",
                    Code = "WEIGHT",
                    Description = "Patient weight",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "kg"
                },

                new ClinicalVariable
                {
                    Name = "Live at nursing home",
                    Code = "NURSING-HOME-RESIDENCE",
                    Description = "Does the patient live at a nursing home",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Has neoplastic disease",
                    Code = "NEOPLASTIC",
                    Description = "Does the patient have neoplastic disease",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Has liver disease history",
                    Code = "LIVER",
                    Description = "Does the patient have liver disease history",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Has congestive heart failure (CHF)",
                    Code = "CHF",
                    Description = "Does the patient have congestive heart failure",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Has cerebrovascular disease history",
                    Code = "CEREBROVASCULAR",
                    Description = "Does the patient have cerebrovascular disease history",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Has renal disease history",
                    Code = "RENAL",
                    Description = "Does the patient have renal disease history",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Confusion",
                    Code = "CONFUSION",
                    Description = "Does the patient have altered mental status",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Respiratory rate",
                    Code = "RR",
                    Description = "Patient respiratory rate",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "breaths/min"
                },

                new ClinicalVariable
                {
                    Name = "Systolic blood pressure",
                    Code = "SBP",
                    Description = "Patient systolic blood pressure",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mmHg"
                },

                new ClinicalVariable
                {
                    Name = "Diastolic blood pressure",
                    Code = "DBP",
                    Description = "Patient diastolic blood pressure",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mmHg"
                },

                new ClinicalVariable
                {
                    Name = "Temperature",
                    Code = "TEMPERATURE",
                    Description = "Patient body temperature",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "Celcius"
                },

                new ClinicalVariable
                {
                    Name = "Pulse rate",
                    Code = "PULSE",
                    Description = "Patient pulse rate",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "bpm"
                },

                new ClinicalVariable
                {
                    Name = "Blood urea nitrogen",
                    Code = "BUN",
                    Description = "Blood urea nitrogen concentration",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mg/dL"
                },

                new ClinicalVariable
                {
                    Name = "pH",
                    Code = "PH",
                    Description = "Patient blood pH",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Hematocrit",
                    Code = "HEMATOCRIT",
                    Description = "Patient hematocrit",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "%"
                },

                new ClinicalVariable
                {
                    Name = "Blood glucose",
                    Code = "GLUCOSE",
                    Description = "Patient blood glucose",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mg/dL"
                },

                new ClinicalVariable
                {
                    Name = "Blood sodium",
                    Code = "NA",
                    Description = "Patient blood sodium concentration",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mmol/L"
                },

                new ClinicalVariable
                {
                    Name = "White blood cell count",
                    Code = "WBC",
                    Description = "Patient white blood cell count",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "cells/mm3"
                },

                new ClinicalVariable
                {
                    Name = "Platelet count",
                    Code = "PLATELET",
                    Description = "Patient platelet count",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "cells/mm3"
                },

                new ClinicalVariable
                {
                    Name = "Partial pressure of arterial oxygen (PaO₂)",
                    Code = "PAO2",
                    Description = "Arterial partial pressure of oxygen",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "mmHg"
                },

                new ClinicalVariable
                {
                    Name = "Fraction of inspired oxygen (FiO₂)",
                    Code = "FIO2",
                    Description = "Fraction of inspired oxygen",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = null // FiO2 is normally used as fraction between 0 and 1 than express as a percentage
                },

                new ClinicalVariable
                {
                    Name = "Peripheral oxygen saturation (SpO2)",
                    Code = "SPO2",
                    Description = "Peripheral oxygen saturation",
                    ValueType = ClinicalValueType.Numeric,
                    CanonicalUnit = "%"
                },

                new ClinicalVariable
                {
                    Name = "Has pleural effusion on X-Ray",
                    Code = "PLEURAL-EFFUSION",
                    Description = "Does the patient have pleural effusion on X-Ray",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Multilobar pulmonary lesions on chest X-ray",
                    Code = "MULTILOBAR-PULMONARY-LESION",
                    Description = "Does the patient have multilobar pulmonary lesions on chest X-ray",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Hypotension requiring aggressive fluid resuscitation",
                    Code = "HYPOTENSION",
                    Description = "Does the patient have hypotension requiring aggressive fluid resuscitation",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Requires mechanical ventilation",
                    Code = "REQUIRES-MECHANICAL-VENTILATION",
                    Description = "Does the patient require invasive mechanical ventilation",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Septic shock requiring vasopressor support",
                    Code = "SEPTIC-SHOCK",
                    Description = "Does the patient have septic shock requiring vasopressors",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                // ---- Risk-factor clinical variables ----
                new ClinicalVariable
                {
                    Name = "Alcohol abuse",
                    Code = "ALCOHOL-ABUSE",
                    Description = "History of alcohol abuse",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Smoking",
                    Code = "SMOKING",
                    Description = "Current or former smoker",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Chronic obstructive pulmonary disease",
                    Code = "COPD",
                    Description = "Has chronic obstructive pulmonary disease (COPD) or other chronic lung disease",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Severe chronic obstructive pulmonary disease",
                    Code = "SEVERE-COPD",
                    Description = "Has severe COPD (FEV1 < 30%)",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Malnutrition",
                    Code = "MALNUTRITION",
                    Description = "Has malnutrition",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Mental illness",
                    Code = "MENTAL-ILLNESS",
                    Description = "Has mental illness",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Immunocompromised",
                    Code = "IMMUNOCOMPROMISED",
                    Description = "Is immunocompromised",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "HIV infection",
                    Code = "HIV",
                    Description = "Has HIV infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Organ transplant",
                    Code = "ORGAN-TRANSPLANT",
                    Description = "Has had an organ transplant",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Diabetes mellitus",
                    Code = "DIABETES",
                    Description = "Has diabetes mellitus",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Influenza infection",
                    Code = "INFLUENZA",
                    Description = "Has influenza infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Measles infection",
                    Code = "MEASLES",
                    Description = "Has measles infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "IV drug use",
                    Code = "IV-DRUG-USE",
                    Description = "History of intravenous drug use",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Prior MRSA infection",
                    Code = "PRIOR-MRSA-INFECTION",
                    Description = "History of MRSA infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Recent hospitalization with IV antibiotics",
                    Code = "HOSPITALIZATION-90D",
                    Description = "Hospitalization and IV antibiotics within 90 days",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "ICU stay",
                    Code = "ICU-STAY",
                    Description = "Recent ICU stay",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Intubation",
                    Code = "INTUBATION",
                    Description = "Recent endotracheal intubation",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Stroke",
                    Code = "STROKE",
                    Description = "Has stroke history",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Epilepsy",
                    Code = "EPILEPSY",
                    Description = "Has epilepsy",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Anesthesia",
                    Code = "ANESTHESIA",
                    Description = "Recent anesthesia",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Prior antibiotic use",
                    Code = "PRIOR-ANTIBIOTIC-USE",
                    Description = "Recent antibiotic use",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Cystic fibrosis",
                    Code = "CYSTIC-FIBROSIS",
                    Description = "Has cystic fibrosis",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Bronchiectasis",
                    Code = "BRONCHIECTASIS",
                    Description = "Has bronchiectasis",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Lung cancer",
                    Code = "LUNG-CANCER",
                    Description = "Has lung cancer",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Prior Pseudomonas infection",
                    Code = "PRIOR-PSEUDOMONAS",
                    Description = "History of Pseudomonas aeruginosa infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Aspiration pneumonia",
                    Code = "ASPIRATION-PNEUMONIA",
                    Description = "Has aspiration pneumonia",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Oropharyngeal anaerobic infection",
                    Code = "OROPHARYNGEAL-ANAEROBIC",
                    Description = "Has oropharyngeal anaerobic infection",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Severe internal medicine disease",
                    Code = "SEVERE-INTERNAL-DISEASE",
                    Description = "Has severe internal medicine disease",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },

                new ClinicalVariable
                {
                    Name = "Pulmonary infarction",
                    Code = "PULMONARY-INFARCTION",
                    Description = "Has pulmonary infarction",
                    ValueType = ClinicalValueType.Boolean,
                    CanonicalUnit = null
                },
            ];

            VariableFormula Var(string code) => new(variables.First(x => x.Code.Equals(code)));

            // CriterionId is just a database FK - the service evaluates the inline
            // Criterion object, so a random ID is fine in tests
            void AddRule(ScoreMetrics metric, string name, Formula criterion, Formula score) =>
                metric.ScoringRules = metric.ScoringRules.Append(new ScoringRule
                {
                    ScoreMetricsId = metric.Id,
                    CriterionId = Guid.CreateVersion7(),
                    Criterion = new Criterion(name, criterion),
                    ScoreFunction = score
                });

            var curb65 = new ScoreMetrics
            {
                Name = "CURB-65",
                Code = "CURB-65",
                Description = "CURB-65",
                ScoringRules = []
            };

            AddRule(curb65, "Does patient have confusion", Eq(Var("CONFUSION"), Bool(true)), Num(1));
            AddRule(curb65, "Patient's urea > 7", Gt(Mul(Var("BUN"), Num(0.357m)), Num(7m)), Num(1));
            AddRule(curb65, "Patient's respiratory rate >= 30", Gte(Var("RR"), Num(30m)), Num(1));
            AddRule(curb65, "Patient's blood pressure", Or(Lt(Var("SBP"), Num(90m)), Lte(Var("DBP"), Num(60m))), Num(1));
            AddRule(curb65, "Patient's age", Gte(Var("AGE"), Num(65m)), Num(1));

            var idsa_ats = new ScoreMetrics
            {
                Name = "IDSA/ATS",
                Code = "IDSA/ATS",
                Description = "IDSA/ATS metrics to assess whether patient need ICU or not",
                ScoringRules = []
            };

            // Major criteria weight 3 points each, minor criteria weight 1 point each.
            // A patient needs ICU when the total score is >= 3 (1 major or 3 minor criteria).
            AddRule(idsa_ats, "Septic shock requiring vasopressor support", Eq(Var("SEPTIC-SHOCK"), Bool(true)), Num(3));
            AddRule(idsa_ats, "Respiratory failure requiring invasive mechanical ventilation", Eq(Var("REQUIRES-MECHANICAL-VENTILATION"), Bool(true)), Num(3));
            AddRule(idsa_ats, "Tachypnea", Gte(Var("RR"), Num(30m)), Num(1));
            AddRule(idsa_ats, "Hypoxia", Lte(Div(Var("PAO2"), Var("FIO2")), Num(250)), Num(1));
            AddRule(idsa_ats, "Radiographic Extension", Eq(Var("MULTILOBAR-PULMONARY-LESION"), Bool(true)), Num(1));
            AddRule(idsa_ats, "Confusion", Eq(Var("CONFUSION"), Bool(true)), Num(1));
            AddRule(idsa_ats, "Uremia", Gte(Var("BUN"), Num(20m)), Num(1));
            AddRule(idsa_ats, "Leukopenia", Lt(Var("WBC"), Num(4000m)), Num(1));
            AddRule(idsa_ats, "Thrombocytopenia", Lt(Var("PLATELET"), Num(100_000m)), Num(1));
            AddRule(idsa_ats, "Hypothermia", Lt(Var("TEMPERATURE"), Num(36m)), Num(1));
            AddRule(idsa_ats, "Hypotension", Eq(Var("HYPOTENSION"), Bool(true)), Num(1));

            var psi = new ScoreMetrics
            {
                Name = "Pneumonia Severity Index",
                Code = "PSI",
                Description = "PSI metrics to assess patient's severity with more detail level",
                ScoringRules = []
            };

            // PSI age factor is special: the score depends on the patient's gender.
            // Since we currently have no way to express a non-criterion factor, we
            // bypass it with an always-true criterion (true = true).
            AddRule(psi, "Age - Sex factor", Eq(Bool(true), Bool(true)),
                new TernaryFormula(
                    Eq(Var("FEMALE"), Bool(true)),
                    Sub(Var("AGE"), Num(10m)),
                    Var("AGE")));
            AddRule(psi, "Nursing home resident", Eq(Var("NURSING-HOME-RESIDENCE"), Bool(true)), Num(10));
            AddRule(psi, "Neoplastic disease history", Eq(Var("NEOPLASTIC"), Bool(true)), Num(30));
            AddRule(psi, "Liver disease history", Eq(Var("LIVER"), Bool(true)), Num(20));
            AddRule(psi, "CHF history", Eq(Var("CHF"), Bool(true)), Num(10));
            AddRule(psi, "Cerebrovascular disease history", Eq(Var("CEREBROVASCULAR"), Bool(true)), Num(10));
            AddRule(psi, "Renal disease history", Eq(Var("RENAL"), Bool(true)), Num(10));
            AddRule(psi, "Altered mental status", Eq(Var("CONFUSION"), Bool(true)), Num(20));
            AddRule(psi, "Respiratory rate ≥ 30 breaths/min", Gte(Var("RR"), Num(30m)), Num(20));
            AddRule(psi, "Systolic blood pressure < 90 mmHg", Lt(Var("SBP"), Num(90m)), Num(20));
            AddRule(psi, "Temperature <35°C (95°F) or >39.9°C (103.8°F)",
                Or(Lt(Var("TEMPERATURE"), Num(35m)), Gte(Var("TEMPERATURE"), Num(40m))), Num(15));
            AddRule(psi, "Pulse ≥ 125 beats/min", Gte(Var("PULSE"), Num(125)), Num(10));
            AddRule(psi, "pH < 7.35", Lt(Var("PH"), Num(7.35m)), Num(30));
            AddRule(psi, "BUN ≥ 30 mg/dL or ≥ 11 mmol/L", Gte(Var("BUN"), Num(30m)), Num(20));
            AddRule(psi, "Sodium < 130 mmol/L", Lt(Var("NA"), Num(130m)), Num(20));
            AddRule(psi, "Glucose ≥ 250 mg/dL or ≥ 14 mmol/L", Gte(Var("GLUCOSE"), Num(250m)), Num(10));
            AddRule(psi, "Hematocrit < 30%", Lt(Var("HEMATOCRIT"), Num(30m)), Num(10));
            AddRule(psi, "Partial pressure of oxygen < 60 mmHg or < 8 kPa",
                Or(Lt(Var("PAO2"), Num(60)), Lt(Var("SPO2"), Num(90))), Num(10));
            AddRule(psi, "Pleural effusion on x-ray", Eq(Var("PLEURAL-EFFUSION"), Bool(true)), Num(10));

            var metrics = new List<ScoreMetrics>() { curb65, idsa_ats, psi };

            // ---- Pathogens, risk factors and suspected causes ----
            // Source: docs/pathogen-risk-factor.pdf
            //   First table      - suspected causes grouped by severity / treatment site
            //   Table 3.2        - risk factors per pathogen.
            //                      Priority follows the table order: first factor = 1 (highest).

            Formula Any(params Formula[] formulas) => formulas.Aggregate(Or);

            var pathogens = new List<Pathogen>();
            Pathogen AddPathogen(string name, string description, bool isAtypical)
            {
                var pathogen = new Pathogen
                {
                    Name = name,
                    Description = description,
                    IsAtypical = isAtypical,
                    RiskFactors = []
                };
                pathogens.Add(pathogen);
                return pathogen;
            }

            // CriterionId / PathogenId are just database FKs - the service evaluates the
            // inline Criterion object, so a random ID is fine in tests.
            void AddRiskFactor(Pathogen pathogen, string name, Formula criterion, int priority) =>
                pathogen.RiskFactors = pathogen.RiskFactors.Append(new RiskFactor
                {
                    PathogenId = pathogen.Id,
                    CriterionId = Guid.CreateVersion7(),
                    Criterion = new Criterion(name, criterion),
                    Priority = priority
                });

            var suspectedCauses = new List<SuspectedCause>();
            void AddSuspectedCause(Pathogen pathogen, Severity severity, TreatmentSite treatmentSite) =>
                suspectedCauses.Add(new SuspectedCause
                {
                    PathogenId = pathogen.Id,
                    Pathogen = pathogen,
                    Severity = severity,
                    TreatmentSite = treatmentSite
                });

            // ---- Pathogens with risk factors (Table 3.2) ----

            // 1. Streptococcus pneumoniae
            var spn = AddPathogen("Streptococcus pneumoniae", "Most common bacterial cause of community-acquired pneumonia", false);
            AddRiskFactor(spn, "Male sex or age < 2 or > 65",
                And(Eq(Var("FEMALE"), Bool(false)), Or(Lt(Var("AGE"), Num(2m)), Gt(Var("AGE"), Num(65m)))), 1);
            AddRiskFactor(spn, "Alcohol abuse or smoking",
                Or(Eq(Var("ALCOHOL-ABUSE"), Bool(true)), Eq(Var("SMOKING"), Bool(true))), 2);
            AddRiskFactor(spn, "Comorbidities (liver, renal, CHF, malnutrition, mental illness, COPD, immunocompromised, HIV, organ transplant)",
                Any(Eq(Var("LIVER"), Bool(true)), Eq(Var("RENAL"), Bool(true)), Eq(Var("CHF"), Bool(true)),
                    Eq(Var("MALNUTRITION"), Bool(true)), Eq(Var("MENTAL-ILLNESS"), Bool(true)), Eq(Var("COPD"), Bool(true)),
                    Eq(Var("IMMUNOCOMPROMISED"), Bool(true)), Eq(Var("HIV"), Bool(true)), Eq(Var("ORGAN-TRANSPLANT"), Bool(true))), 3);

            // 2. Haemophilus influenzae
            var hflu = AddPathogen("Haemophilus influenzae", "Common pathogen in patients with chronic lung disease", false);
            AddRiskFactor(hflu, "Chronic lung disease", Eq(Var("COPD"), Bool(true)), 1);
            AddRiskFactor(hflu, "Malignancy", Eq(Var("NEOPLASTIC"), Bool(true)), 2);
            AddRiskFactor(hflu, "HIV infection", Eq(Var("HIV"), Bool(true)), 3);
            AddRiskFactor(hflu, "Alcohol abuse", Eq(Var("ALCOHOL-ABUSE"), Bool(true)), 4);
            AddRiskFactor(hflu, "Smoking", Eq(Var("SMOKING"), Bool(true)), 5);

            // 3. Staphylococcus aureus
            var saureus = AddPathogen("Staphylococcus aureus", "Common in structural lung disease or post-viral infection", false);
            AddRiskFactor(saureus, "COPD, lung cancer or cystic fibrosis",
                Any(Eq(Var("COPD"), Bool(true)), Eq(Var("LUNG-CANCER"), Bool(true)), Eq(Var("CYSTIC-FIBROSIS"), Bool(true))), 1);
            AddRiskFactor(saureus, "Diabetes or renal failure",
                Any(Eq(Var("DIABETES"), Bool(true)), Eq(Var("RENAL"), Bool(true))), 2);
            AddRiskFactor(saureus, "Influenza or measles infection",
                Any(Eq(Var("INFLUENZA"), Bool(true)), Eq(Var("MEASLES"), Bool(true))), 3);
            AddRiskFactor(saureus, "IV drug use", Eq(Var("IV-DRUG-USE"), Bool(true)), 4);

            // 4. Methicillin-Resistant Staphylococcus aureus (MRSA)
            var mrsa = AddPathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)", "Healthcare-associated resistant staph", false);
            AddRiskFactor(mrsa, "Prior MRSA infection", Eq(Var("PRIOR-MRSA-INFECTION"), Bool(true)), 1);
            AddRiskFactor(mrsa, "Hospitalization and IV antibiotics within 90 days", Eq(Var("HOSPITALIZATION-90D"), Bool(true)), 2);

            // 5. Klebsiella pneumoniae
            var klebsiella = AddPathogen("Klebsiella pneumoniae", "Enteric Gram-negative bacillus associated with aspiration", false);
            AddRiskFactor(klebsiella, "ICU stay or intubation",
                Any(Eq(Var("ICU-STAY"), Bool(true)), Eq(Var("INTUBATION"), Bool(true))), 1);
            AddRiskFactor(klebsiella, "Aspiration risk (stroke, epilepsy, anesthesia)",
                Any(Eq(Var("STROKE"), Bool(true)), Eq(Var("EPILEPSY"), Bool(true)), Eq(Var("ANESTHESIA"), Bool(true))), 2);
            AddRiskFactor(klebsiella, "Alcohol abuse", Eq(Var("ALCOHOL-ABUSE"), Bool(true)), 3);
            AddRiskFactor(klebsiella, "Chronic lung disease or diabetes",
                Any(Eq(Var("COPD"), Bool(true)), Eq(Var("DIABETES"), Bool(true))), 4);
            AddRiskFactor(klebsiella, "Prior antibiotic use", Eq(Var("PRIOR-ANTIBIOTIC-USE"), Bool(true)), 5);

            // 6. Pseudomonas aeruginosa
            var pseudomonas = AddPathogen("Pseudomonas aeruginosa", "Pathogen in structural lung disease and prior Pseudomonas infection", false);
            AddRiskFactor(pseudomonas, "Structural lung disease (cystic fibrosis, bronchiectasis, severe COPD)",
                Any(Eq(Var("CYSTIC-FIBROSIS"), Bool(true)), Eq(Var("BRONCHIECTASIS"), Bool(true)), Eq(Var("SEVERE-COPD"), Bool(true))), 1);
            AddRiskFactor(pseudomonas, "Prior Pseudomonas aeruginosa infection", Eq(Var("PRIOR-PSEUDOMONAS"), Bool(true)), 2);
            AddRiskFactor(pseudomonas, "Hospitalization and IV antibiotics within 90 days", Eq(Var("HOSPITALIZATION-90D"), Bool(true)), 3);

            // 7. Acinetobacter baumannii
            var acinetobacter = AddPathogen("Acinetobacter baumannii", "Multidrug-resistant pathogen in severe illness", false);
            AddRiskFactor(acinetobacter, "Alcohol abuse, old age or severe internal medicine disease",
                Any(Eq(Var("ALCOHOL-ABUSE"), Bool(true)), Gt(Var("AGE"), Num(65m)), Eq(Var("SEVERE-INTERNAL-DISEASE"), Bool(true))), 1);

            // 8. Anaerobic bacteria
            var anaerobic = AddPathogen("Anaerobic bacteria", "Associated with aspiration pneumonia", false);
            AddRiskFactor(anaerobic, "Lung disease (lung cancer, bronchiectasis, pulmonary infarction, aspiration pneumonia)",
                Any(Eq(Var("LUNG-CANCER"), Bool(true)), Eq(Var("BRONCHIECTASIS"), Bool(true)),
                    Eq(Var("PULMONARY-INFARCTION"), Bool(true)), Eq(Var("ASPIRATION-PNEUMONIA"), Bool(true))), 1);
            AddRiskFactor(anaerobic, "Oropharyngeal anaerobic infection", Eq(Var("OROPHARYNGEAL-ANAEROBIC"), Bool(true)), 2);

            // ---- Pathogens from Table 1 with no specific risk factors in Table 3.2 ----
            var moraxella = AddPathogen("Moraxella catarrhalis", "Common in patients with chronic lung disease", false);
            var mycoplasma = AddPathogen("Mycoplasma pneumoniae", "Atypical pathogen, common in young adults", true);
            var chlamydia = AddPathogen("Chlamydia pneumoniae", "Atypical pathogen, often co-infection", true);
            var legionella = AddPathogen("Legionella spp", "Atypical pathogen, severe CAP", true);
            var bordetella = AddPathogen("Bordetella pertussis", "Causes pertussis-associated pneumonia", false);
            // Enteric Gram-negative bacilli (Table 1) expanded into specific pathogens
            var ecoli = AddPathogen("Escherichia coli (E. coli)", "Enteric Gram-negative bacillus", false);
            var salmonella = AddPathogen("Salmonella", "Enteric Gram-negative bacillus", false);
            var enterobacter = AddPathogen("Enterobacter", "Enteric Gram-negative bacillus", false);

            // ---- Respiratory viruses (Table 1) ----
            var influenza = AddPathogen("Influenza virus", "Most important respiratory viral pathogen", false);
            var parainfluenza = AddPathogen("Parainfluenza virus", "Respiratory virus", false);
            var rsv = AddPathogen("Respiratory syncytial virus (RSV)", "Respiratory virus", false);
            var adenovirus = AddPathogen("Adenovirus", "Respiratory virus", false);
            var coronavirus = AddPathogen("Coronavirus", "Respiratory virus", false);
            var sarsCov2 = AddPathogen("SARS-CoV-2", "Causative agent of COVID-19", false);
            var rhinovirus = AddPathogen("Rhinovirus", "Respiratory virus", false);
            var enterovirus = AddPathogen("Enterovirus", "Respiratory virus", false);
            var bocavirus = AddPathogen("Bocavirus", "Respiratory virus", false);
            var metapneumovirus = AddPathogen("Metapneumovirus", "Respiratory virus", false);
            var hmpv = AddPathogen("Human metapneumovirus (HMPV)", "Respiratory virus", false);
            var cmv = AddPathogen("Cytomegalovirus (CMV)", "Respiratory virus", false);
            var ebv = AddPathogen("Epstein-Barr virus (EBV)", "Respiratory virus", false);

            // ---- Suspected causes (first table) ----
            // Group A: mild / outpatient
            foreach (var p in new[] { spn, hflu, moraxella, mycoplasma, chlamydia,
                influenza, parainfluenza, rsv, adenovirus, coronavirus, sarsCov2,
                rhinovirus, enterovirus, bocavirus })
            {
                AddSuspectedCause(p, Severity.Mild, TreatmentSite.Outpatient);
            }

            // Group B: moderate / inpatient
            foreach (var p in new[] { spn, mycoplasma, chlamydia, hflu, ecoli, salmonella,
                klebsiella, enterobacter, anaerobic, legionella, bordetella, influenza,
                parainfluenza, rsv, enterovirus, rhinovirus, adenovirus, coronavirus, sarsCov2, bocavirus,
                metapneumovirus, hmpv, cmv, ebv })
            {
                AddSuspectedCause(p, Severity.Moderate, TreatmentSite.Inpatient);
                AddSuspectedCause(p, Severity.Severe, TreatmentSite.Inpatient);
            }

            // Group C: severe / ICU
            foreach (var p in new[] { spn, ecoli, salmonella, klebsiella, enterobacter, saureus,
                legionella, mycoplasma, pseudomonas, influenza, parainfluenza, rsv, adenovirus,
                coronavirus, sarsCov2, metapneumovirus, hmpv })
            {
                AddSuspectedCause(p, Severity.Severe, TreatmentSite.IntensiveCareUnit);
            }

            return new ClinicalContext
            {
                Variables = variables,
                Metrics = metrics,
                Pathogens = pathogens,
                SuspectedCauses = suspectedCauses,
            };
        }

        private static ClinicalVariable GetVariable(string code)
        {
            return _context.Variables.First(x => x.Code.Equals(code));
        }

        // ---- Formula builders ----

        private static BooleanConstantFormula Bool(bool value) => new(value);
        private static NumericConstantFormula Num(decimal value) => new(value);
        private static BinaryFormula Eq(Formula left, Formula right) => new(left, right, ExpressionOperator.EQ);
        private static BinaryFormula Gt(Formula left, Formula right) => new(left, right, ExpressionOperator.GT);
        private static BinaryFormula Gte(Formula left, Formula right) => new(left, right, ExpressionOperator.GTE);
        private static BinaryFormula Lt(Formula left, Formula right) => new(left, right, ExpressionOperator.LT);
        private static BinaryFormula Lte(Formula left, Formula right) => new(left, right, ExpressionOperator.LTE);
        private static BinaryFormula Mul(Formula left, Formula right) => new(left, right, ExpressionOperator.MUL);
        private static BinaryFormula Div(Formula left, Formula right) => new(left, right, ExpressionOperator.DIV);
        private static BinaryFormula Sub(Formula left, Formula right) => new(left, right, ExpressionOperator.SUB);
        private static BinaryFormula Or(Formula left, Formula right) => new(left, right, ExpressionOperator.OR);
        private static BinaryFormula And(Formula left, Formula right) => new(left, right, ExpressionOperator.AND);

        // ---- Observation / case builders ----

        private static ClinicalObservation Obs(string code, decimal value) => new(GetVariable(code), value);
        private static ClinicalObservation Obs(string code, bool value) => new(GetVariable(code), value);

        // Baseline: 42-year-old male with normal vitals, no CURB-65 criterion matched
        private static List<ClinicalObservation> CurbCase(
            decimal age = 42m,
            bool confusion = false,
            decimal bun = 15m,
            decimal rr = 20m,
            decimal sbp = 124m,
            decimal dbp = 76m) =>
        [
            Obs("AGE", age),
            Obs("CONFUSION", confusion),
            Obs("BUN", bun),
            Obs("RR", rr),
            Obs("SBP", sbp),
            Obs("DBP", dbp),
        ];

        // Baseline: 42-year-old male with normal labs, only the age factor scores (42)
        private static List<ClinicalObservation> PsiCase(
            decimal age = 42m,
            bool female = false,
            bool nursingHome = false,
            bool neoplastic = false,
            bool liver = false,
            bool chf = false,
            bool cerebrovascular = false,
            bool renal = false,
            bool confusion = false,
            decimal rr = 20m,
            decimal sbp = 124m,
            decimal temperature = 38.2m,
            decimal pulse = 96m,
            decimal bun = 15m,
            decimal ph = 7.43m,
            decimal hematocrit = 42m,
            decimal glucose = 150m,
            decimal sodium = 139m,
            decimal pao2 = 82m,
            decimal spo2 = 96m,
            bool pleuralEffusion = false) =>
        [
            Obs("AGE", age),
            Obs("FEMALE", female),
            Obs("NURSING-HOME-RESIDENCE", nursingHome),
            Obs("NEOPLASTIC", neoplastic),
            Obs("LIVER", liver),
            Obs("CHF", chf),
            Obs("CEREBROVASCULAR", cerebrovascular),
            Obs("RENAL", renal),
            Obs("CONFUSION", confusion),
            Obs("RR", rr),
            Obs("SBP", sbp),
            Obs("TEMPERATURE", temperature),
            Obs("PULSE", pulse),
            Obs("BUN", bun),
            Obs("PH", ph),
            Obs("HEMATOCRIT", hematocrit),
            Obs("GLUCOSE", glucose),
            Obs("NA", sodium),
            Obs("PAO2", pao2),
            Obs("SPO2", spo2),
            Obs("PLEURAL-EFFUSION", pleuralEffusion),
        ];

        // Baseline: all IDSA/ATS variables present, no criterion matched.
        // A null parameter omits the observation, so rules using that variable
        // cannot be evaluated and are skipped (contribute 0).
        private static List<ClinicalObservation> IdsaCase(
            bool? multilobar = false,
            bool? hypotension = false,
            bool? ventilation = false,
            bool? septicShock = false,
            decimal? pao2 = 82m,
            decimal? fio2 = 0.21m,
            decimal? rr = 20m,
            bool? confusion = false,
            decimal? bun = 15m,
            decimal? wbc = 12400m,
            decimal? platelet = 245_000m,
            decimal? temperature = 38.2m)
        {
            List<ClinicalObservation> observations = [];
            if (multilobar.HasValue) observations.Add(Obs("MULTILOBAR-PULMONARY-LESION", multilobar.Value));
            if (hypotension.HasValue) observations.Add(Obs("HYPOTENSION", hypotension.Value));
            if (ventilation.HasValue) observations.Add(Obs("REQUIRES-MECHANICAL-VENTILATION", ventilation.Value));
            if (septicShock.HasValue) observations.Add(Obs("SEPTIC-SHOCK", septicShock.Value));
            if (pao2.HasValue) observations.Add(Obs("PAO2", pao2.Value));
            if (fio2.HasValue) observations.Add(Obs("FIO2", fio2.Value));
            if (rr.HasValue) observations.Add(Obs("RR", rr.Value));
            if (confusion.HasValue) observations.Add(Obs("CONFUSION", confusion.Value));
            if (bun.HasValue) observations.Add(Obs("BUN", bun.Value));
            if (wbc.HasValue) observations.Add(Obs("WBC", wbc.Value));
            if (platelet.HasValue) observations.Add(Obs("PLATELET", platelet.Value));
            if (temperature.HasValue) observations.Add(Obs("TEMPERATURE", temperature.Value));
            return observations;
        }

        // ---- Test data ----

        public static TheoryData<List<ClinicalObservation>, int> curbData =
        [
            // 0 - No criteria
            new(CurbCase(), 0),

            // 1 - Confusion only
            new(CurbCase(confusion: true), 1),

            // 1 - BUN only (20 * 0.357 = 7.14 > 7)
            new(CurbCase(bun: 20m), 1),

            // 1 - Respiratory rate only
            new(CurbCase(rr: 30m), 1),

            // 1 - Systolic hypotension only
            new(CurbCase(sbp: 89m), 1),

            // 1 - Diastolic hypotension only
            new(CurbCase(dbp: 60m), 1),

            // 1 - Age only
            new(CurbCase(age: 65m), 1),

            // 2 - Confusion + BUN
            new(CurbCase(confusion: true, bun: 25m), 2),

            // 2 - RR + Age
            new(CurbCase(age: 65m, rr: 32m), 2),

            // SBP + DBP hypotension condition
            // Both are positive, but the hypotension criterion must only contribute 1 point.
            new(CurbCase(sbp: 89m, dbp: 60m), 1),

            // 3 - Confusion + BUN + RR
            new(CurbCase(confusion: true, bun: 25m, rr: 30m), 3),

            // 3 - Age + RR + hypotension
            new(CurbCase(age: 70m, rr: 35m, sbp: 88m), 3),

            // 4 - Confusion + BUN + RR + Age
            new(CurbCase(age: 72m, confusion: true, bun: 25m, rr: 32m), 4),

            // 4 - All except confusion
            new(CurbCase(age: 72m, bun: 25m, rr: 32m, sbp: 85m, dbp: 55m), 4),

            // 5 - All CURB-65 criteria positive
            new(CurbCase(age: 78m, confusion: true, bun: 28m, rr: 34m, sbp: 86m, dbp: 54m), 5),

            // Boundary - RR 29 should NOT score
            new(CurbCase(rr: 29m), 0),

            // Boundary - RR 30 SHOULD score
            new(CurbCase(rr: 30m), 1),

            // Boundary - SBP 90 should NOT score
            new(CurbCase(sbp: 90m), 0),

            // Boundary - SBP 89 SHOULD score
            new(CurbCase(sbp: 89m), 1),

            // Boundary - DBP 61 should NOT score
            new(CurbCase(dbp: 61m), 0),

            // Boundary - DBP 60 SHOULD score
            new(CurbCase(dbp: 60m), 1),

            // Boundary - Age 64 should NOT score
            new(CurbCase(age: 64m), 0),

            // Boundary - Age 65 SHOULD score
            new(CurbCase(age: 65m), 1),
        ];

        public static TheoryData<List<ClinicalObservation>, int> psiData =
        [
            // 0. Baseline - no risk factors
            new(PsiCase(), 42),

            // 1. Female -10
            new(PsiCase(female: true), 32),

            // 2. Nursing home +10
            new(PsiCase(age: 72m, nursingHome: true), 82),

            // 3. Confusion +20
            new(PsiCase(confusion: true), 62),

            // 4. Respiratory rate >= 30 +20
            new(PsiCase(rr: 30m), 62),

            // 5. SBP < 90 +20
            new(PsiCase(sbp: 89m), 62),

            // 6. Temperature < 35 +15
            new(PsiCase(temperature: 34.9m), 57),

            // 7. Temperature >= 40 +15
            new(PsiCase(temperature: 40m), 57),

            // 8. Pulse >= 125 +10
            new(PsiCase(pulse: 125m), 52),

            // 9. BUN >= 30 mg/dL +20
            new(PsiCase(bun: 30m), 62),

            // 10. pH < 7.35 +30
            new(PsiCase(ph: 7.34m), 72),

            // 11. Sodium < 130 +20
            new(PsiCase(sodium: 129m), 62),

            // 12. Glucose >= 250 mg/dL +10
            new(PsiCase(glucose: 250m), 52),

            // 13. Hematocrit < 30% +10
            new(PsiCase(hematocrit: 29m), 52),

            // 14. PaO2 < 60 +10
            new(PsiCase(pao2: 59m), 52),

            // 15. SpO2 < 90% +10
            new(PsiCase(spo2: 89m), 52),

            // 16. Pleural effusion +10
            new(PsiCase(pleuralEffusion: true), 52),

            // 17. Multiple risk factors:
            // 62 (female 72 - 10) + 10 nursing home + 10 CHF + 10 renal
            // + 20 RR 32 + 10 pulse 128 = 122
            new(PsiCase(age: 72m, female: true, nursingHome: true, chf: true, renal: true, rr: 32m, pulse: 128m), 122),

            // 18. Comprehensive high-risk patient:
            // 72 (female 82 - 10) + 10 nursing home + 30 neoplastic + 20 liver + 10 CHF
            // + 10 cerebrovascular + 10 renal + 20 confusion + 20 RR 34 + 20 SBP 82
            // + 15 temperature 40.2 + 10 pulse 132 + 20 BUN 35 + 30 pH 7.28
            // + 10 hematocrit 27 + 10 glucose 275 + 20 sodium 126 + 10 PaO2 55
            // + 10 pleural effusion = 357
            new(PsiCase(
                age: 82m, female: true, nursingHome: true, neoplastic: true, liver: true,
                chf: true, cerebrovascular: true, renal: true, confusion: true, rr: 34m,
                sbp: 82m, temperature: 40.2m, pulse: 132m, bun: 35m, ph: 7.28m,
                hematocrit: 27m, glucose: 275m, sodium: 126m, pao2: 55m, spo2: 86m,
                pleuralEffusion: true), 357),
        ];

        public static TheoryData<List<ClinicalObservation>, int> idsa_atsData =
        [
            // Baseline: all variables present, 0 criteria
            new(IdsaCase(), 0),

            // Multilobar pulmonary lesion
            new(IdsaCase(multilobar: true), 1),

            // RR >= 30
            new(IdsaCase(rr: 30m), 1),

            // PaO2 / FiO2 <= 250
            new(IdsaCase(pao2: 50m), 1),

            // Confusion
            new(IdsaCase(confusion: true), 1),

            // BUN >= 20
            new(IdsaCase(bun: 20m), 1),

            // WBC < 4000
            new(IdsaCase(wbc: 3999m), 1),

            // Platelet < 100000
            new(IdsaCase(platelet: 99_999m), 1),

            // Temperature < 36
            new(IdsaCase(temperature: 35.9m), 1),

            // Hypotension
            new(IdsaCase(hypotension: true), 1),

            // All 9 minor criteria
            new(IdsaCase(
                multilobar: true, hypotension: true, pao2: 50m, rr: 35m, confusion: true,
                bun: 30m, wbc: 3500m, platelet: 80_000m, temperature: 35m), 9),

            // Both major criteria (3 points each) = 6
            new(IdsaCase(ventilation: true, septicShock: true), 6),

            // Missing PAO2: oxygenation criterion cannot be evaluated,
            // but all other criteria are evaluated.
            new(IdsaCase(pao2: null, rr: 30m, bun: 20m), 2),

            // Missing FIO2: oxygenation criterion cannot be evaluated.
            new(IdsaCase(multilobar: true, pao2: 50m, fio2: null, rr: 30m), 2),

            // Missing both PAO2 and FIO2.
            // Oxygenation criterion cannot be evaluated.
            new(IdsaCase(multilobar: true, pao2: null, fio2: null, rr: 30m, confusion: true, bun: 20m), 4),

            // Missing several unrelated observations.
            // Remaining positive criteria are still counted.
            new(IdsaCase(multilobar: true, pao2: null, fio2: null, rr: 32m, confusion: true, bun: null, wbc: null, platelet: null), 3),

            // Only major criteria available.
            // Both major criteria are satisfied (3 points each) = 6.
            new(IdsaCase(
                multilobar: null, hypotension: null, ventilation: true, septicShock: true,
                pao2: null, fio2: null, rr: null, confusion: null, bun: null, wbc: null,
                platelet: null, temperature: null), 6),

            // Only minor criteria available: RR 32 + confusion + BUN 25 = 3
            new(IdsaCase(
                multilobar: null, hypotension: null, ventilation: null, septicShock: null,
                pao2: null, fio2: null, rr: 32m, confusion: true, bun: 25m, wbc: null,
                platelet: null, temperature: null), 3),
        ];

        public static TheoryData<List<ClinicalObservation>, Severity, TreatmentSite> successDiagnosis =
        [
            // Healthy 42-year-old male: CURB-65 = 0, PSI = 42, IDSA/ATS = 0
            new(
            [
                ..PsiCase(),
                Obs("HEIGHT", 172m),
                Obs("WEIGHT", 70m),
                Obs("DBP", 76m),
                Obs("WBC", 12400m),
                Obs("PLATELET", 245_000m),
                Obs("FIO2", 0.21m),
            ], Severity.Mild, TreatmentSite.Outpatient),
            // Test the case where higest severity and treatment site would be prioritized
            new(
            [
                // age + confusion -> CURB-65 = 2 => moderate + inpatient
                // age + female + neoplastic + septic-shock -> PSI = 100 => severe + inpatient
                // septic-shock -> IDSA/ATS = 3 => need ICU
                Obs("CONFUSION", true),
                Obs("AGE", 70m),
                Obs("FEMALE", false),
                Obs("NEOPLASTIC", true),
                Obs("SEPTIC-SHOCK", true),
            ], Severity.Severe, TreatmentSite.IntensiveCareUnit),

            // Test CRB-65 case
            new(
            [
                // If CURB-65, the result should be mild + outpatient
                Obs("CONFUSION", true),
            ], Severity.Moderate, TreatmentSite.Inpatient),

        ];

        public static TheoryData<List<ClinicalObservation>, Severity, TreatmentSite, List<HeavySuspected>, List<Pathogen>> assessInfectionData =
        [
            new(
                [],
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [
                    _context.Pathogens.First(x => x.Name.Equals("Streptococcus pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Haemophilus influenzae")),
                    _context.Pathogens.First(x => x.Name.Equals("Moraxella catarrhalis")),
                    _context.Pathogens.First(x => x.Name.Equals("Mycoplasma pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Chlamydia pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Influenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Parainfluenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Respiratory syncytial virus (RSV)")),
                    _context.Pathogens.First(x => x.Name.Equals("Enterovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Rhinovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Adenovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Coronavirus")),
                    _context.Pathogens.First(x => x.Name.Equals("SARS-CoV-2")),
                    _context.Pathogens.First(x => x.Name.Equals("Bocavirus")),
                ]
            ),
            new(
                [
                    // Pseudomonas risk factors
                    Obs("HOSPITALIZATION-90D", true),
                    Obs("PRIOR-PSEUDOMONAS", true)
                ],
                Severity.Mild,
                TreatmentSite.Outpatient,
                [
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Pseudomonas aeruginosa")), (1m / 3m) + 0.5m),
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Methicillin-Resistant Staphylococcus aureus (MRSA)")), 1m / 2m),
                ],
                [
                    _context.Pathogens.First(x => x.Name.Equals("Streptococcus pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Haemophilus influenzae")),
                    _context.Pathogens.First(x => x.Name.Equals("Moraxella catarrhalis")),
                    _context.Pathogens.First(x => x.Name.Equals("Mycoplasma pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Chlamydia pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Influenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Parainfluenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Respiratory syncytial virus (RSV)")),
                    _context.Pathogens.First(x => x.Name.Equals("Enterovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Rhinovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Adenovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Coronavirus")),
                    _context.Pathogens.First(x => x.Name.Equals("SARS-CoV-2")),
                    _context.Pathogens.First(x => x.Name.Equals("Bocavirus")),
                ]
            ),
            new(
                [
                    // With age being > 65, Streptococcus pneumoniae and Acinetobacter baumannii
                    // should be considered as a heavy suspected.
                    // With alcohol abuse, Streptococcus pneumoniae, Haemophilus influenzae,
                    // Klebsiella pneumoniae and Acinetobacter baumannii should be considered as
                    // a heavy suspected.
                    Obs("AGE", 70m),
                    Obs("FEMALE", false),
                    Obs("ALCOHOL-ABUSE", true),
                    Obs("SMOKING", false),
                    Obs("SEVERE-INTERNAL-DISEASE", false),
                ],
                Severity.Mild,
                TreatmentSite.Outpatient,
                [
                    // Score = sum(1st_priority_score + 2nd) + 1 = 1 + 1/2 + 1 = 2.5
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Streptococcus pneumoniae")), 2.5m),
                    // Score = sum(3rd_priority_score) = 1/3
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Klebsiella pneumoniae")), 1m / 3m),
                    // Score = sum(1st_priority_score) = 1
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Acinetobacter baumannii")), 1m),
                    // Score = sum(4th_priority_score) + 1 = 1/4 + 1 = 1.25
                    new HeavySuspected(_context.Pathogens.First(x => x.Name.Equals("Haemophilus influenzae")), 1.25m),
                ],
                [
                    // _context.Pathogens.First(x => x.Name.Equals("Streptococcus pneumoniae")),
                    // _context.Pathogens.First(x => x.Name.Equals("Haemophilus influenzae")),
                    _context.Pathogens.First(x => x.Name.Equals("Moraxella catarrhalis")),
                    _context.Pathogens.First(x => x.Name.Equals("Mycoplasma pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Chlamydia pneumoniae")),
                    _context.Pathogens.First(x => x.Name.Equals("Influenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Parainfluenza virus")),
                    _context.Pathogens.First(x => x.Name.Equals("Respiratory syncytial virus (RSV)")),
                    _context.Pathogens.First(x => x.Name.Equals("Enterovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Rhinovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Adenovirus")),
                    _context.Pathogens.First(x => x.Name.Equals("Coronavirus")),
                    _context.Pathogens.First(x => x.Name.Equals("SARS-CoV-2")),
                    _context.Pathogens.First(x => x.Name.Equals("Bocavirus")),
                ]
            ),

        ];

        // ---- Tests ----

        [Theory]
        [MemberData(nameof(curbData))]
        public void DiagnoseSeverityTest_Curb65_Success(List<ClinicalObservation> observations, int curbScore)
        {
            // Assert CURB-65 score
            var curb65 = _context.Metrics.First(x => x.Code.Equals("CURB-65"));
            Assert.Equal(curbScore, _service.CalculateMetricsScore(curb65, observations));
        }

        [Theory]
        [MemberData(nameof(psiData))]
        public void DiagnoseSeverityTest_Psi_Success(List<ClinicalObservation> observations, int psiScore)
        {
            // Assert PSI score
            var psi = _context.Metrics.First(x => x.Code.Equals("PSI"));
            Assert.Equal(psiScore, _service.CalculateMetricsScore(psi, observations));
        }

        [Theory]
        [MemberData(nameof(idsa_atsData))]
        public void DiagnoseSeverityTest_Idsa_Ats_Success(List<ClinicalObservation> observations, int idsa_atsScore)
        {
            // Assert IDSA/ATS score
            var idsa_ats = _context.Metrics.First(x => x.Code.Equals("IDSA/ATS"));
            Assert.Equal(idsa_atsScore, _service.CalculateMetricsScore(idsa_ats, observations));
        }

        [Theory]
        [MemberData(nameof(successDiagnosis))]
        public void DiagnoseSeverityTest_Diagnose_Success(List<ClinicalObservation> observations, Severity expectedSeverity, TreatmentSite expectedTreatmentSite)
        {
            var result = _service.DiagnoseSeverity(observations);
            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedSeverity, result.Data.Severity);
            Assert.Equal(expectedTreatmentSite, result.Data.TreatmentSite);
            Assert.NotEmpty(result.Data.MetricsDiagnoses);
        }

        [Theory]
        [MemberData(nameof(assessInfectionData))]
        public void AssessInfectionTest_Success(List<ClinicalObservation> observations, Severity expectedSeverity, TreatmentSite expectedTreatmentSite, List<HeavySuspected> heavySuspected, List<Pathogen> pathogens)
        {
            var result = _service.AssessInfection(expectedSeverity, expectedTreatmentSite, observations);
            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Equal(heavySuspected.Count, result.Data.HeavySuspected.Count());
            foreach (var hs in heavySuspected)
            {
                // Check if the expected heavy suspected is contained in the result
                Assert.Contains(hs, result.Data.HeavySuspected);

                var resultHs = Assert.Single(result.Data.HeavySuspected, x => x.Pathogen.Id == hs.Pathogen.Id);
                Assert.Equal(hs.PriorityScore, resultHs.PriorityScore, 4);
            }

            // Check if the expected pathogens are contained in the result
            Assert.Equal(pathogens.Count, result.Data.WorthSuspected.Count());
            foreach (var p in pathogens)
            {
                Assert.Contains(p, result.Data.WorthSuspected);
            }
        }
    }
}
