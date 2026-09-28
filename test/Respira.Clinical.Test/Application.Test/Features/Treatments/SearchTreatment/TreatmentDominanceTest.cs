using Respira.Clinical.Application.Features.Treatments.SearchTreatment;
using Xunit;

namespace Respira.Application.Test.Features.Treatments.SearchTreatment
{
    public class TreatmentDominanceTest
    {
        private static readonly Guid _pseudomonas = Guid.Parse("00000000-0000-0000-0000-500000000006");
        private static readonly Guid _saureus = Guid.Parse("00000000-0000-0000-0000-500000000003");
        private static readonly Guid _klebsiella = Guid.Parse("00000000-0000-0000-0000-500000000005");
        private static readonly Guid _criterion = Guid.Parse("00000000-0000-0000-0000-000000000001");

        private static TreatmentCandidate Candidate(Guid id, Guid[] pathogens)
        {
            return new TreatmentCandidate(id, [.. pathogens], [], []);
        }

        private static TreatmentCandidate Candidate(Guid id, Guid[] pathogens, Guid[] criteria)
        {
            return new TreatmentCandidate(id, [.. pathogens], [.. criteria], []);
        }

        [Fact]
        public void Filter_CoinfectionWithSaureus_HidesSinglePathogenTreatment_Success()
        {
            // Pseudomonas-only treatment must be hidden in favour of Pseudomonas + S. aureus
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus]);

            var result = TreatmentDominance.Filter([single, combo], [_pseudomonas, _saureus]);

            var survivor = Assert.Single(result);
            Assert.Equal(combo.Id, survivor.Id);
        }

        [Fact]
        public void Filter_CoinfectionWithSaureus_HidesBothSinglePathogenTreatments_Success()
        {
            // Both Pseudomonas-only and S. aureus-only treatments are hidden by the combo
            var pseudoOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var aureusOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_saureus]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000003"), [_pseudomonas, _saureus]);

            var result = TreatmentDominance.Filter([pseudoOnly, aureusOnly, combo], [_pseudomonas, _saureus]);

            var survivor = Assert.Single(result);
            Assert.Equal(combo.Id, survivor.Id);
        }

        [Fact]
        public void Filter_NoComboTreatment_KeepsSinglePathogenTreatments_Success()
        {
            // Fallback: without a combo, the single-pathogen treatments stay visible
            var pseudoOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var aureusOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_saureus]);

            var result = TreatmentDominance.Filter([pseudoOnly, aureusOnly], [_pseudomonas, _saureus]);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_NestedPathogenSets_KeepsWidestTreatment_Success()
        {
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus]);
            var widest = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000003"), [_pseudomonas, _saureus, _klebsiella]);

            var result = TreatmentDominance.Filter([single, combo, widest], [_pseudomonas, _saureus, _klebsiella]);

            var survivor = Assert.Single(result);
            Assert.Equal(widest.Id, survivor.Id);
        }

        [Fact]
        public void Filter_UnrelatedPathogenTreatments_BothKept_Success()
        {
            // Neither treatment covers the other's pathogens, so both stay as fallback
            var pseudoOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var klebsiellaOnly = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_klebsiella]);

            var result = TreatmentDominance.Filter([pseudoOnly, klebsiellaOnly], [_pseudomonas, _saureus]);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_SingleSelectedPathogen_DominanceNotApplied_Success()
        {
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus]);

            var result = TreatmentDominance.Filter([single, combo], [_pseudomonas]);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_NoSelectedPathogen_DominanceNotApplied_Success()
        {
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus]);

            var result = TreatmentDominance.Filter([single, combo], []);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_ComboRequiresExtraCriterion_SingleKept_Success()
        {
            // The combo is not applicable whenever the single is, so nothing is hidden
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus], [_criterion]);

            var result = TreatmentDominance.Filter([single, combo], [_pseudomonas, _saureus]);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Filter_SingleRequiresExtraCriterion_HidesSinglePathogenTreatment_Success()
        {
            // The combo has no extra criteria, so it is applicable wherever the single is
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), [_pseudomonas], [_criterion]);
            var combo = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas, _saureus]);

            var result = TreatmentDominance.Filter([single, combo], [_pseudomonas, _saureus]);

            var survivor = Assert.Single(result);
            Assert.Equal(combo.Id, survivor.Id);
        }

        [Fact]
        public void Filter_TreatmentWithoutPathogen_HiddenByAnyMatch_Success()
        {
            // A treatment that specifies no pathogen adds nothing once a covered one matches
            var noPathogen = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000001"), []);
            var single = Candidate(Guid.Parse("00000000-0000-0000-0000-900000000002"), [_pseudomonas]);

            var result = TreatmentDominance.Filter([noPathogen, single], [_pseudomonas, _saureus]);

            var survivor = Assert.Single(result);
            Assert.Equal(single.Id, survivor.Id);
        }
    }
}
