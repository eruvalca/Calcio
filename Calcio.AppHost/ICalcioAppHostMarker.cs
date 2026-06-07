namespace Calcio.AppHost;

/// <summary>
/// Marker interface that exposes a public type from the AppHost assembly.
/// Integration-test fixtures pass this type to
/// <see cref="Aspire.Hosting.Testing.DistributedApplicationTestingBuilder.CreateAsync{TEntryPoint}"/>
/// so the testing builder can locate the AppHost assembly and invoke its entry point.
/// </summary>
public interface ICalcioAppHostMarker
{
}
