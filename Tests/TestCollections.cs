using Xunit;

namespace Nast.SimpleMediator.Tests;

/// <summary>
/// Global collection to ensure all tests run sequentially and don't interfere with each other.
/// </summary>
[CollectionDefinition("GlobalTestCollection")]
public class GlobalTestCollection : ICollectionFixture<GlobalTestCollection>
{
}

/// <summary>
/// Collection for unit tests that need isolation.
/// </summary>
[CollectionDefinition("UnitTests")]
public class UnitTestCollection : ICollectionFixture<UnitTestCollection>
{
}

/// <summary>
/// Collection for integration tests that need isolation.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestCollection>
{
}

/// <summary>
/// Collection for performance tests that need isolation.
/// </summary>
[CollectionDefinition("PerformanceTests")]
public class PerformanceTestCollection : ICollectionFixture<PerformanceTestCollection>
{
}
