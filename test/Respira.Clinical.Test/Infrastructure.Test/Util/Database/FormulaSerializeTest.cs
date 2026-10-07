using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Util.Database;
using Xunit;

namespace Respira.Clinical.Infrastructure.Test.Util.Database
{
    public class FormulaSerializerTest
    {
        private static readonly VariableRef s_bun = new(Guid.CreateVersion7(), "BUN", ClinicalValueType.Numeric);

        private static Formula RoundTrip(Formula formula)
        {
            var json = FormulaSerializer.Serialize(formula);
            var back = FormulaSerializer.Deserialize(json);

            Assert.Equal(json, FormulaSerializer.Serialize(back));
            Assert.Equal(formula.ToString(), back.ToString());

            return back;
        }

        [Fact]
        public void NumericConstant_RoundTrips()
        {
            var back = (NumericConstantFormula)RoundTrip(new NumericConstantFormula(20.5m));

            Assert.Equal(20.5m, back.Constant);
        }

        [Fact]
        public void BooleanConstant_RoundTrips()
        {
            var back = (BooleanConstantFormula)RoundTrip(new BooleanConstantFormula(true));

            Assert.True(back.Constant);
        }

        [Fact]
        public void CategoricalConstant_RoundTrips()
        {
            var back = (CategoricalConstantFormula)RoundTrip(new CategoricalConstantFormula("PNEUMOCOCCAL"));

            Assert.Equal("PNEUMOCOCCAL", back.Constant);
        }

        [Fact]
        public void Variable_RoundTripsAsVariableRef()
        {
            var back = (VariableFormula)RoundTrip(new VariableFormula(s_bun));

            Assert.Equal(s_bun.Id, back.Variable.Id);
            Assert.Equal("BUN", back.Variable.Code);
            Assert.Equal(ClinicalValueType.Numeric, back.Variable.ValueType);
        }

        [Fact]
        public void Unary_RoundTrips()
        {
            var back = (UnaryFormula)RoundTrip(new UnaryFormula(new BooleanConstantFormula(false)));

            Assert.IsType<BooleanConstantFormula>(back.Formula);
        }

        [Fact]
        public void Binary_RoundTrips()
        {
            var back = (BinaryFormula)RoundTrip(
                new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(20), ExpressionOperator.GT));

            Assert.Equal(ExpressionOperator.GT, back.Operator);
            Assert.IsType<VariableFormula>(back.Left);
            Assert.IsType<NumericConstantFormula>(back.Right);
        }

        [Fact]
        public void Ternary_RoundTrips()
        {
            var condition = new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(10), ExpressionOperator.GT);
            var ifTrue = new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(1), ExpressionOperator.ADD);
            var ifFalse = new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(2), ExpressionOperator.ADD);

            var back = (TernaryFormula)RoundTrip(new TernaryFormula(condition, ifTrue, ifFalse));

            Assert.IsType<BinaryFormula>(back.Condition);
            Assert.IsType<BinaryFormula>(back.IfTrue);
            Assert.IsType<BinaryFormula>(back.IfFalse);
        }

        [Fact]
        public void NestedTree_RoundTrips()
        {
            var left = new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(20), ExpressionOperator.ADD);
            var right = new TernaryFormula(
                new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(10), ExpressionOperator.GT),
                new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(1), ExpressionOperator.ADD),
                new BinaryFormula(new VariableFormula(s_bun), new NumericConstantFormula(2), ExpressionOperator.ADD));

            var back = (BinaryFormula)RoundTrip(new BinaryFormula(left, right, ExpressionOperator.ADD));

            Assert.Equal(ExpressionOperator.ADD, back.Operator);

            // Every node references the same variable; Variables dedupes by code.
            Assert.Single(back.Variables);
        }

        [Fact]
        public void Serialize_WritesDiscriminatorAndStableVariablePayload_WithoutDerivedBloat()
        {
            var json = FormulaSerializer.Serialize(new VariableFormula(s_bun));

            Assert.StartsWith("{\"$type\":\"variable\"", json);
            Assert.Contains("\"code\":\"BUN\"", json);
            Assert.Contains("\"valueType\":\"Numeric\"", json);

            // Derived runtime data must never reach the stored document
            Assert.DoesNotContain("\"variables\"", json);
            Assert.DoesNotContain("\"resultType\"", json);

            // Entity-only members must never reach the stored document
            Assert.DoesNotContain("\"acceptedRange\"", json);
            Assert.DoesNotContain("\"isRequired\"", json);
        }

        [Fact]
        public void Deserialize_DiscriminatorNotFirst_StillDeserializes()
        {
            // Postgres jsonb reorders object keys on storage (shorter keys first), so the
            // "$type" discriminator ends up after "left" in binary nodes:
            // {"left": {...}, "$type": "binary", "right": {...}, "operator": "GT"}
            const string json = "{\"left\": {\"$type\": \"numeric\", \"constant\": 1}, \"$type\": \"binary\", " +
                "\"right\": {\"$type\": \"numeric\", \"constant\": 2}, \"operator\": \"GT\"}";

            var back = FormulaSerializer.Deserialize(json);

            var binary = Assert.IsType<BinaryFormula>(back);
            Assert.Equal(ExpressionOperator.GT, binary.Operator);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(binary.Left).Constant);
            Assert.Equal(2m, Assert.IsType<NumericConstantFormula>(binary.Right).Constant);
        }

        [Fact]
        public void Deserialize_NestedNodesReorderedByJsonb_StillDeserializes()
        {
            // jsonb reorders keys per object: the ternary root keeps "$type" first but its
            // binary children get "left"/"right" ahead of it, and "variable" ahead of "$type".
            const string json = "{\"$type\": \"ternary\", " +
                "\"ifTrue\": {\"$type\": \"numeric\", \"constant\": 1}, \"ifFalse\": {\"$type\": \"numeric\", \"constant\": 2}, " +
                "\"condition\": {\"left\": {\"$type\": \"variable\", \"variable\": {\"id\": \"00000000-0000-0000-0000-000000000000\", \"code\": \"BUN\", \"valueType\": \"Numeric\"}}, " +
                "\"$type\": \"binary\", \"right\": {\"$type\": \"numeric\", \"constant\": 10}, \"operator\": \"GT\"}}";

            var back = FormulaSerializer.Deserialize(json);

            var ternary = Assert.IsType<TernaryFormula>(back);
            var condition = Assert.IsType<BinaryFormula>(ternary.Condition);
            Assert.Equal(ExpressionOperator.GT, condition.Operator);
            Assert.Equal("BUN", Assert.IsType<VariableFormula>(condition.Left).Variable.Code);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(ternary.IfTrue).Constant);
        }

        [Fact]
        public void Deserialize_MissingDiscriminator_Throws()
        {
            // Pre-$type (legacy) payloads are unsupported by design: the dev database is reset
            // and re-seeded, so no shape-sniffing fallback is kept around.
            Assert.ThrowsAny<NotSupportedException>(() =>
                FormulaSerializer.Deserialize("{\"constant\":1}"));
        }
    }

}
