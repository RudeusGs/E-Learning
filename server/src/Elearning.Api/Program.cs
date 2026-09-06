using DotNetEnv;
using Elearning.Api.Extensions;
using Elearning.Infrastructure;
using Elearning.Infrastructure.Persistence;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Environment, app.Configuration);
app.UseApiPipeline();
app.Run();

public partial class Program { }
