var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("18-3.6")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("matchingsports");

var migrator = builder.AddProject<Projects.MatchingSports_Migrator>("migrator")
    .WithReference(database)
    .WaitFor(database);

var api = builder.AddProject<Projects.MatchingSports_Api>("api")
    .WithReference(database)
    .WaitForCompletion(migrator)
    .WithEnvironment("Database__Initialize", "false")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
