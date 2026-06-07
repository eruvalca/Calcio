/// <summary>
/// Configures and runs the Aspire AppHost for local Calcio development resources.
/// </summary>
internal static class AppHostProgram
{
    /// <summary>
    /// Starts the AppHost and provisions dependent resources for the Calcio application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the host process.</param>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // DistributedApplicationTestingBuilder sets the environment name to "Testing".
        // In testing mode we skip fixed container names, fixed host ports, and data volumes
        // so each test run gets isolated, ephemeral resources that clean up automatically.
        var isTesting = builder.Environment.EnvironmentName == "Testing";

        // In testing mode use static placeholder credentials; containers are ephemeral
        // and never exposed outside the test process so security is not a concern.
        var pgUser = isTesting
            ? builder.AddParameter("postgres-user", "testuser", publishValueAsDefault: false, secret: false)
            : builder.AddParameter("postgres-user", secret: false);

        var pgPassword = isTesting
            ? builder.AddParameter("postgres-password", "testpassword", publishValueAsDefault: false, secret: false)
            : builder.AddParameter("postgres-password", secret: false);

        var postgres = builder.AddPostgres("postgres", pgUser, pgPassword)
            .WithImageTag("17.7");

        if (isTesting)
        {
            // Session lifetime: Aspire tears the container down when the test host stops.
            // No fixed name or port so parallel test runs do not conflict.
            postgres.WithLifetime(ContainerLifetime.Session);
        }
        else
        {
            postgres
                .WithContainerName("postgres")
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .WithEndpoint("tcp", endpoint =>
                {
                    endpoint.Port = 15432;
                    endpoint.TargetPort = 5432;
                    endpoint.IsProxied = false;
                });
        }

        var postgresDb = postgres.AddDatabase("calcioDb");

        var storage = builder.AddAzureStorage("storage");

        if (isTesting)
        {
            // Session lifetime: no fixed name, no data volume, no fixed ports.
            storage.RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Session));
        }
        else
        {
            storage.RunAsEmulator(emulator =>
            {
                // Persistent lifetime with Azurite is stable only when host ports are fixed.
                emulator.WithContainerName("azurite");
                emulator.WithDataVolume();
                emulator.WithBlobPort(26000);
                emulator.WithQueuePort(26001);
                emulator.WithTablePort(26002);
                emulator.WithLifetime(ContainerLifetime.Persistent);
            });
        }

        var blobStorage = storage.AddBlobs("blobs");

        builder.AddProject<Projects.Calcio>("calcio")
            .WithReference(postgresDb, connectionName: "DefaultConnection")
            .WithReference(blobStorage)
            .WaitFor(blobStorage)
            .WaitFor(postgres)
            .WaitFor(postgresDb)
            .WithUrls(context =>
            {
                // Remove HTTP URLs and customize HTTPS URL.
                context.Urls.RemoveAll(url => url.Endpoint?.Scheme == "http");

                foreach (var url in context.Urls)
                {
                    if (url.Endpoint?.Scheme == "https")
                    {
                        url.DisplayText = "Calcio";
                    }
                }

                // Add Scalar link.
                context.Urls.Add(new() { Endpoint = context.GetEndpoint("https"), Url = "/scalar/v1", DisplayText = "Scalar" });
            });

        builder.Build().Run();
    }
}
