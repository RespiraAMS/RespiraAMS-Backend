using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;

namespace Respira.Clinical.Domain.Test.Services
{
    public class DiagnoseServiceTest
    {
        private readonly DiagnoseService _service;

        public DiagnoseServiceTest()
        {
            var logger = new Mock<ILogger<DiagnoseService>>().Object;
            _service = new DiagnoseService(logger);

        }

        // Initialize clinical context
        // To avoid duplicate code, we will use the seed data logic from Infrastructure
        // to seed the clinical context
        private async Task<ClinicalContext> CreateContextAsync()
        {
            var seedData = await DataSeeder.LoadAsync("seed-data-test.jsonc");
            return new ClinicalContext
            {
                Antibiotics = seedData.Antibiotics,
                Metrics = seedData.ClinicalMetrics,
                Pathogens = seedData.Pathogens,
                SuspectedCauses = seedData.SuspectedCauses,
                AntibioticGroups = seedData.AntibioticGroups,
                Variables = seedData.ClinicalVariables,
            };
        }
    }
}
