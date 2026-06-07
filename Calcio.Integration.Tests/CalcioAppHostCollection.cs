using Aspire.Hosting.Testing;

namespace Calcio.Integration.Tests;

/// <summary>
/// xUnit collection definition that shares a single <see cref="CalcioAppHostFixture"/> instance
/// across all tests in the collection, avoiding redundant AppHost start/stop cycles.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CalcioAppHostCollection : ICollectionFixture<CalcioAppHostFixture>
{
    /// <summary>
    /// The collection name used on <c>[Collection]</c> attributes in test classes.
    /// </summary>
    public const string Name = "CalcioAppHost";
}
