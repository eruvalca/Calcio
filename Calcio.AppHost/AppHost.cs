var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("postgres-user", secret: false);
var pgPassword = builder.AddParameter("postgres-password", secret: false);

var postgres = builder.AddPostgres("postgres", pgUser, pgPassword)
    .WithImageTag("17.7")
    .WithContainerName("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.Port = 15432;
        endpoint.TargetPort = 5432;
        endpoint.IsProxied = false;
    });

var postgresDb = postgres.AddDatabase("calcioDb");

var blobStorage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithContainerName("azurite")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume())
    .AddBlobs("blobs");

builder.AddProject<Projects.Calcio>("calcio")
    .WithReference(postgresDb, connectionName: "DefaultConnection")
    .WithReference(blobStorage)
    .WaitFor(blobStorage)
    .WaitFor(postgres)
    .WaitFor(postgresDb)
    .WithUrls(context =>
    {
        // Remove HTTP URLs and customize HTTPS URL
        context.Urls.RemoveAll(url => url.Endpoint?.Scheme == "http");

        foreach (var url in context.Urls)
        {
            if (url.Endpoint?.Scheme == "https")
            {
                url.DisplayText = "Calcio";
            }
        }

        // Add Scalar link
        context.Urls.Add(new() { Endpoint = context.GetEndpoint("https"), Url = "/scalar/v1", DisplayText = "Scalar" });
    });

builder.Build().Run();
