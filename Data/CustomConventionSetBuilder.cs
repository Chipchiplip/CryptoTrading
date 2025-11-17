using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace CryptoTrading.Data
{
    public class CustomConventionSetBuilder : IConventionSetBuilder
    {
        private readonly IConventionSetBuilder _baseBuilder;

        public CustomConventionSetBuilder(IConventionSetBuilder baseBuilder)
        {
            _baseBuilder = baseBuilder;
        }

        public ConventionSet CreateConventionSet()
        {
            var conventionSet = _baseBuilder.CreateConventionSet();
            
            // Remove ElementMappingConvention to avoid FindCollectionMapping errors
            conventionSet.ModelFinalizingConventions.RemoveAll(c => c is ElementMappingConvention);
            
            return conventionSet;
        }
    }
}

