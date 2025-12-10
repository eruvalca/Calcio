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
        .WithBlobPort(27000)
        .WithQueuePort(27001)
        .WithTablePort(27002)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume())
    .AddBlobs("blobs");

builder.AddProject<Projects.Calcio>("calcio")
    .WithReference(postgresDb, connectionName: "DefaultConnection")
    .WithReference(blobStorage)
    .WaitFor(blobStorage)
    .WaitFor(postgres)
    .WaitFor(postgresDb);

builder.Build().Run();
