var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("postgres-user", secret: false);
var pgPassword = builder.AddParameter("postgres-password", secret: false);

var postgres = builder.AddPostgres("postgres", pgUser, pgPassword)
    .WithImageTag("17.7")
    .WithContainerName("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres.AddDatabase("calcioDb");

builder.AddProject<Projects.Calcio>("calcio")
    .WithReference(postgresDb, connectionName: "DefaultConnection")
    .WaitFor(postgresDb);

builder.Build().Run();
