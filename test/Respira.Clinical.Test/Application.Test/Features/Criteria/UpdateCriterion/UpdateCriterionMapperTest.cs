using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionMapperTest
    {
        private readonly IUpdateMapper<Criterion, UpdateCriterionCommand> _mapper =
            new UpdateCriterionMapper(new FormulaMapper());

        #region Name and formula mapping

        [Fact]
        public void MapModel_BooleanConstant_Success()
        {
            var model = CreateAgeCriterion();
            var originalId = model.Id;
            var command = CreateCommand(
                "Universal triage screening", new FormulaDto { Constant = "false" });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal("Universal triage screening", model.Name);
            var formula = Assert.IsType<BooleanConstantFormula>(model.Formula);
            Assert.False(formula.Constant);

            // Update rewrites the payload only: the identity must survive
            Assert.Equal(originalId, model.Id);
        }

        [Fact]
        public void MapModel_Variable_Success()
        {
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "FEMALE");
            var model = CreateAgeCriterion();
            var command = CreateCommand(
                "Female-specific risk", new FormulaDto { Variable = female.Id });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { female });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal("Female-specific risk", model.Name);
            var formula = Assert.IsType<VariableFormula>(model.Formula);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("FEMALE", formula.Variable.Code);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
        }

        [Fact]
        public void MapModel_Comparison_Success()
        {
            // LOINC equivalent of the seed data: SBP below the 90 mmHg hypotension threshold
            var sbp = CreateNumericVariable(Guid.CreateVersion7(), "SBP", "mmHg");
            var model = CreateAgeCriterion();
            var command = CreateCommand(
                "Hypotension",
                new FormulaDto
                {
                    Operator = ExpressionOperator.LT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "90" },
                });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { sbp });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var formula = Assert.IsType<BinaryFormula>(model.Formula);
            Assert.Equal(ExpressionOperator.LT, formula.Operator);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            Assert.Equal(90m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal("SBP < 90", formula.ToString());
        }

        [Fact]
        public void MapModel_NestedLogical_Success()
        {
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "FEMALE");
            var sbp = CreateNumericVariable(Guid.CreateVersion7(), "SBP", "mmHg");
            var model = CreateAgeCriterion();
            var command = CreateCommand(
                "Female hypertensive crisis",
                new FormulaDto
                {
                    Operator = ExpressionOperator.AND,
                    Left = new FormulaDto { Variable = female.Id },
                    Right = new FormulaDto
                    {
                        Operator = ExpressionOperator.GT,
                        Left = new FormulaDto { Variable = sbp.Id },
                        Right = new FormulaDto { Constant = "140" },
                    },
                });

            var result = _mapper.MapModel(
                model, command, new List<ClinicalVariable> { female, sbp });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var formula = Assert.IsType<BinaryFormula>(model.Formula);
            Assert.Equal(ExpressionOperator.AND, formula.Operator);
            // Only the non-leaf operand is parenthesized
            Assert.Equal("FEMALE AND (SBP > 140)", formula.ToString());

            // Variables are de-duplicated by code
            Assert.Equal(2, model.Variables.Count());
            Assert.Contains(model.Variables, x => x.Code == "FEMALE");
            Assert.Contains(model.Variables, x => x.Code == "SBP");
        }

        [Fact]
        public void MapModel_KeepsIdentityAndState_Success()
        {
            // Update rewrites name and formula only: the soft delete state, the create
            // timestamp and the association rows keyed on the id must stay intact
            var model = CreateAgeCriterion();
            var originalId = model.Id;
            var originalCreatedAt = model.CreatedAt;
            var command = CreateCommand(
                "Tuổi > 65", new FormulaDto { Constant = "true" });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(originalId, model.Id);
            Assert.Equal(originalCreatedAt, model.CreatedAt);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        [Fact]
        public void MapModel_UpdatesNameAndTimestamp_Success()
        {
            var model = CreateAgeCriterion();
            // A fixed old timestamp makes the "moved forward" assertion deterministic
            var stale = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            model.UpdatedAt = stale;
            var command = CreateCommand(
                "Tuổi > 65", new FormulaDto { Constant = "true" });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.True(
                model.UpdatedAt > stale,
                $"UpdatedAt {model.UpdatedAt} should move past the stale value {stale}");
        }

        #endregion

        #region Fail path

        [Fact]
        public void MapModel_NonBooleanFormula_Failure()
        {
            /*
             * Business rule: a criterion formula must evaluate to a boolean value, so a
             * purely numeric formula cannot be stored on a criterion
             */
            var age = CreateNumericVariable(Guid.CreateVersion7(), "AGE", "year");
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var originalFormula = model.Formula;
            var originalUpdatedAt = model.UpdatedAt;
            var command = CreateCommand(
                "Age in years",
                new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Variable = age.Id },
                    Right = new FormulaDto { Constant = "65" },
                });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { age });

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The rejected update must leave the model untouched
            Assert.Equal(originalName, model.Name);
            Assert.Equal(originalFormula, model.Formula);
            Assert.Equal(originalUpdatedAt, model.UpdatedAt);
        }

        [Fact]
        public void MapModel_UnknownVariable_Failure()
        {
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var command = CreateCommand(
                "Female-specific risk", new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(originalName, model.Name);
        }

        [Fact]
        public void MapModel_EmptyFormula_Failure()
        {
            // Boundary: every field null cannot be resolved into any formula type
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var command = CreateCommand("Tuổi >= 65", new FormulaDto());

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(originalName, model.Name);
        }

        [Fact]
        public void MapModel_InvalidFormula_Failure()
        {
            // Boolean and numeric operands make the domain model throw, which the
            // formula mapper turns into a failure result
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var command = CreateCommand(
                "Age in years",
                new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "65" },
                });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(originalName, model.Name);
        }

        #endregion

        #region Invalid dependencies and command

        [Fact]
        public void MapModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the recursive formula tree,
            // it only exists to satisfy the mapper interface
            var model = CreateAgeCriterion();
            var command = CreateCommand("Tuổi > 65", new FormulaDto { Constant = "true" });

            Assert.Throws<NotImplementedException>(() => _mapper.MapModel(model, command));
        }

        [Fact]
        public void MapModel_NullDependencies_Failure()
        {
            // The dependency list is how variable ids get resolved, so without it no
            // formula can be built
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var command = CreateCommand("Tuổi > 65", new FormulaDto { Constant = "true" });

            var result = _mapper.MapModel(model, command, null);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(originalName, model.Name);
        }

        [Fact]
        public void MapModel_NonVariableListDependencies_Failure()
        {
            var model = CreateAgeCriterion();
            var originalName = model.Name;
            var command = CreateCommand("Tuổi > 65", new FormulaDto { Constant = "true" });

            var result = _mapper.MapModel(model, command, new List<string> { "FEMALE" });

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(originalName, model.Name);
        }

        #endregion

        private static UpdateCriterionCommand CreateCommand(string name, FormulaDto formula) =>
            new() { Id = Guid.CreateVersion7(), Name = name, Formula = formula };

        private static Criterion CreateAgeCriterion() =>
            new("Tuổi >= 65", new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric)),
                new NumericConstantFormula(65),
                ExpressionOperator.GTE));

        private static BooleanClinicalVariable CreateBooleanVariable(Guid id, string code)
        {
            return new BooleanClinicalVariable
            {
                Id = id,
                Code = code,
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            };
        }

        private static NumericClinicalVariable CreateNumericVariable(
            Guid id, string code, string unit)
        {
            var (name, description) = code == "AGE"
                ? ("Tuổi", "Patient age in years")
                : ("Huyết áp tâm thu", "Systolic blood pressure measured at the arm");

            return new NumericClinicalVariable
            {
                Id = id,
                Code = code,
                Name = name,
                Description = description,
                CanonicalUnit = unit,
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = code == "AGE" ? 120 : 300,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
                NormalRange = code == "AGE" ? null : new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
            };
        }
    }
}
