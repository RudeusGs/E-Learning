using System.Text.Json;
using System.Text.Json.Serialization;
using Elearning.Api.Errors;
using Elearning.Api.Security;
using Elearning.Application.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace Elearning.Api.DependencyInjectionModules;

internal static class PresentationRegistration
{
    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        AddControllers(services);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
        });
        AddProblemDetails(services);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton<RefreshTokenCookie>();
        services.AddSingleton<AuthSessionHttpAdapter>();
        ConfigureModelValidation(services);
        return services;
    }

    private static void AddControllers(IServiceCollection services) =>
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

    private static void AddProblemDetails(IServiceCollection services) =>
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Type ??= ProblemDetailsMetadata.HttpErrorType;
                context.ProblemDetails.Extensions.TryAdd(
                    ProblemDetailsMetadata.TraceIdExtension,
                    context.HttpContext.TraceIdentifier);
                context.ProblemDetails.Extensions.TryAdd(
                    ProblemDetailsMetadata.CodeExtension,
                    ErrorCodes.HttpError);
            });

    private static void ConfigureModelValidation(IServiceCollection services) =>
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Type = ProblemDetailsMetadata.ValidationFailedType,
                    Title = ProblemDetailsMetadata.ValidationFailedTitle,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions =
                    {
                        [ProblemDetailsMetadata.CodeExtension] = ErrorCodes.ValidationFailed,
                        [ProblemDetailsMetadata.TraceIdExtension] = context.HttpContext.TraceIdentifier
                    }
                };
                return new BadRequestObjectResult(problem);
            };
        });
}
