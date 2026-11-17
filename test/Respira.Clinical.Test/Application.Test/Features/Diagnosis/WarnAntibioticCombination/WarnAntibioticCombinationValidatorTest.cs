using Respira.Clinical.Application.Features.Diagnosis.WarnAntibioticCombination;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.WarnAntibioticCombination
{
    public class WarnAntibioticCombinationValidatorTest
    {
        private readonly WarnAntibioticCombinationValidator _validator = new();

        #region Valid query

        [Fact]
        public async Task WarnAntibioticCombination_SingleAntibiotic_Success()
        {
            // Boundary: 1 antibiotic is the smallest accepted custom combination
            var query = CreateQuery(Guid.CreateVersion7());

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task WarnAntibioticCombination_TwoDistinctAntibiotics_Success()
        {
            // Conventional moderate CAP therapy: a third-generation cephalosporin
            // plus a macrolide
            var query = CreateQuery(Guid.CreateVersion7(), Guid.CreateVersion7());

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task WarnAntibioticCombination_ThreeDistinctAntibiotics_Success()
        {
            // Boundary: 3 antibiotics is the largest realistic custom combination
            var query = CreateQuery(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task WarnAntibioticCombination_EmptyGuid_Success()
        {
            // Boundary: the validator only enforces a non-empty and distinct list,
            // existence of each antibiotic is enforced by the handler against the DB
            var query = CreateQuery(Guid.Empty);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid query

        [Fact]
        public async Task WarnAntibioticCombination_EmptyAntibioticList_Fail()
        {
            // Boundary: 0 antibiotics is the lower boundary of the list
            var query = CreateQuery();

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        /*
         * The query is deserialized from an HTTP body, so an explicit JSON null for
         * the antibiotic list is representable (the required modifier is a compile-time
         * contract, System.Text.Json only rejects a *missing* member). Validation
         * must report an error instead of throwing.
         */
        [Fact]
        public async Task WarnAntibioticCombination_NullAntibioticList_Fail()
        {
            var query = new WarnAntibioticCombinationQuery
            {
                AntibioticIds = null!,
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task WarnAntibioticCombination_DuplicateAntibiotic_Fail()
        {
            // Boundary: 2 entries with the same ID is the smallest duplicated list
            var id = Guid.CreateVersion7();
            var query = CreateQuery(id, id);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task WarnAntibioticCombination_DuplicateAmongThreeAntibiotics_Fail()
        {
            var first = Guid.CreateVersion7();
            var repeated = Guid.CreateVersion7();
            var query = CreateQuery(first, repeated, first);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task WarnAntibioticCombination_AllDuplicates_Fail()
        {
            // Boundary: 3 entries with the same ID contains no distinct antibiotic
            var id = Guid.CreateVersion7();
            var query = CreateQuery(id, id, id);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        #endregion

        private static WarnAntibioticCombinationQuery CreateQuery(params Guid[] antibioticIds)
        {
            return new WarnAntibioticCombinationQuery
            {
                AntibioticIds = [.. antibioticIds],
            };
        }
    }
}
