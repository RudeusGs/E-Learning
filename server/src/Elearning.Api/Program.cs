using DotNetEnv;
using Elearning.Api.Extensions;
using Elearning.Infrastructure;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline();
app.Run();

public partial class Program { }
