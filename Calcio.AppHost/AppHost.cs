var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Calcio>("calcio");

builder.Build().Run();
