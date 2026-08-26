using Xunit;

namespace Elearning.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresIntegrationGroup : ICollectionFixture<IntegrationTestFactory>
{
    public const string Name = "PostgreSQL integration";
}
