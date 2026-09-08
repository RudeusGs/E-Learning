using Elearning.Api.Controllers;
using Elearning.Application.Auth;
using Elearning.Domain;
using Elearning.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Elearning.UnitTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ProjectReferencesRespectCleanArchitectureDirection()
    {
        AssertDoesNotReference(typeof(Course).Assembly, "Elearning.Application", "Elearning.Infrastructure", "Elearning.Api");
        AssertDoesNotReference(typeof(LoginCommandHandler).Assembly, "Elearning.Infrastructure", "Elearning.Api");
        AssertDoesNotReference(typeof(JwtOptions).Assembly, "Elearning.Api");
    }

    [Fact]
    public void ControllersDoNotDependOnInfrastructureImplementations()
    {
        var controllerTypes = typeof(AuthController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        foreach (var controllerType in controllerTypes)
        {
            var infrastructureFields = controllerType
                .GetFields(System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public)
                .Where(field => field.FieldType.Namespace?.StartsWith("Elearning.Infrastructure", StringComparison.Ordinal) == true)
                .Select(field => field.FieldType.FullName)
                .ToArray();

            Assert.True(
                infrastructureFields.Length == 0,
                $"{controllerType.Name} directly depends on Infrastructure types: {string.Join(", ", infrastructureFields)}");
        }
    }

    [Fact]
    public void ControllerActionsDoNotExposeApplicationOrDomainModels()
    {
        foreach (var method in ControllerTypes().SelectMany(type => type.GetMethods(
                     System.Reflection.BindingFlags.Instance |
                     System.Reflection.BindingFlags.Public |
                     System.Reflection.BindingFlags.DeclaredOnly)))
        {
            foreach (var parameter in method.GetParameters())
            {
                AssertContractType(method, parameter.ParameterType, allowSimpleType: true);
            }

            AssertContractType(method, method.ReturnType, allowSimpleType: false);
        }
    }

    [Fact]
    public void ControllersContainNoPersistenceOrBusinessQueryCode()
    {
        var controllerDirectory = Path.Combine(SolutionRoot(), "server", "src", "Elearning.Api", "Controllers");
        var forbiddenFragments = new[]
        {
            "ElearningDbContext",
            "DbSet<",
            "UserManager<",
            ".Select(",
            ".Where(",
            ".Any(",
            ".SingleOrDefault",
            ".ExecuteUpdate",
            "TokenSecurity.",
            "JwtAccessTokenGenerator"
        };

        foreach (var file in Directory.EnumerateFiles(controllerDirectory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            foreach (var fragment in forbiddenFragments)
            {
                Assert.DoesNotContain(fragment, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void ProductionClassesStayBelowThreeHundredLines()
    {
        var sourceRoot = Path.Combine(SolutionRoot(), "server", "src");
        var oversizedFiles = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => new { Path = path, Lines = File.ReadLines(path).Count() })
            .Where(file => file.Lines > 300)
            .ToArray();

        Assert.True(
            oversizedFiles.Length == 0,
            $"Source files over 300 lines: {string.Join(", ", oversizedFiles.Select(file => $"{file.Path} ({file.Lines})"))}");
    }

    [Fact]
    public void FeatureUseCasesUseCommandOrQueryHandlerNames()
    {
        var applicationTypes = typeof(LoginCommandHandler).Assembly.GetTypes();
        var inconsistentContracts = applicationTypes
            .Where(type => type.IsInterface)
            .Where(type => type.Namespace?.StartsWith("Elearning.Application", StringComparison.Ordinal) == true)
            .Where(type => type.Namespace?.Contains(".Ports", StringComparison.Ordinal) != true)
            .Where(type => type.Namespace?.Contains(".Security", StringComparison.Ordinal) != true)
            .Where(type => type.Namespace?.Contains(".Interfaces", StringComparison.Ordinal) != true)
            .Where(type => type.GetMethods().Any(method => typeof(Task).IsAssignableFrom(method.ReturnType) ||
                                                           method.ReturnType.IsGenericType &&
                                                           method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            .Where(type => !type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) &&
                           !type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(
            inconsistentContracts.Length == 0,
            $"Use-case contracts must use command/query handler naming: {string.Join(", ", inconsistentContracts)}");
    }

    [Fact]
    public void AllUseCaseHandlersExposeExecuteAsyncOnly()
    {
        var inconsistentMethods = typeof(LoginCommandHandler).Assembly
            .GetTypes()
            .Where(type => type.IsInterface &&
                           (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                            type.Name.EndsWith("QueryHandler", StringComparison.Ordinal)))
            .SelectMany(type => type.GetMethods().Select(method => new { Type = type, Method = method }))
            .Where(item => item.Method.Name != "ExecuteAsync")
            .Select(item => $"{item.Type.FullName}.{item.Method.Name}")
            .ToArray();

        Assert.True(
            inconsistentMethods.Length == 0,
            $"Use-case handlers must expose ExecuteAsync: {string.Join(", ", inconsistentMethods)}");
    }

    [Fact]
    public void UseCaseFilesFollowOneFlatCommandQueryConvention()
    {
        var applicationRoot = Path.Combine(SolutionRoot(), "server", "src", "Elearning.Application");
        var invalidFiles = Directory
            .EnumerateDirectories(applicationRoot, "UseCases", SearchOption.AllDirectories)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Select(path => new { Directory = directory, Path = path }))
            .Where(file => !string.Equals(
                               Path.GetDirectoryName(file.Path),
                               file.Directory,
                               StringComparison.OrdinalIgnoreCase) ||
                           !HasUseCaseFileName(Path.GetFileName(file.Path)))
            .Select(file => file.Path)
            .ToArray();

        Assert.True(
            invalidFiles.Length == 0,
            $"Use-case files must be flat and use command/query naming: {string.Join(", ", invalidFiles)}");
    }

    [Fact]
    public void ProductionTypesUseResponsibilityRevealingNames()
    {
        var permittedServiceNames = new[]
        {
            "ISessionLogoutService",
            "ICacheService",
            "RedisCacheService",
            nameof(SessionLogoutService),
            nameof(AuthTokenCleanupService)
        };
        var ambiguousTypes = new[]
            {
                typeof(Course).Assembly,
                typeof(LoginCommandHandler).Assembly,
                typeof(JwtOptions).Assembly,
                typeof(AuthController).Assembly
            }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.StartsWith("Elearning", StringComparison.Ordinal) == true)
            .Where(type => type.Name.EndsWith("Manager", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Guard", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Helper", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Utils", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Common", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Processor", StringComparison.Ordinal) ||
                           type.Name.EndsWith("Service", StringComparison.Ordinal) &&
                           !permittedServiceNames.Contains(type.Name, StringComparer.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(
            ambiguousTypes.Length == 0,
            $"Replace ambiguous production type names: {string.Join(", ", ambiguousTypes)}");
    }

    [Fact]
    public void ProductionCodeDoesNotThrowBaseExceptionTypes()
    {
        var sourceRoot = Path.Combine(SolutionRoot(), "server", "src");
        var forbiddenFragments = new[]
        {
            "throw new Exception(",
            "throw new System.Exception(",
            "throw new ApplicationException("
        };
        var violations = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .SelectMany(path => forbiddenFragments
                .Where(fragment => File.ReadAllText(path).Contains(fragment, StringComparison.Ordinal))
                .Select(fragment => $"{path}: {fragment}"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Use typed domain or application exceptions: {string.Join(", ", violations)}");
    }

    private static IEnumerable<Type> ControllerTypes() =>
        typeof(AuthController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

    private static bool HasUseCaseFileName(string fileName) =>
        fileName.EndsWith("Command.cs", StringComparison.Ordinal) ||
        fileName.EndsWith("Query.cs", StringComparison.Ordinal) ||
        fileName.EndsWith("CommandHandler.cs", StringComparison.Ordinal) ||
        fileName.EndsWith("QueryHandler.cs", StringComparison.Ordinal);

    private static void AssertContractType(
        System.Reflection.MethodInfo action,
        Type type,
        bool allowSimpleType)
    {
        if (type == typeof(void) || type == typeof(CancellationToken) ||
            typeof(IActionResult).IsAssignableFrom(type) ||
            allowSimpleType && (type.IsPrimitive || type.IsEnum || type == typeof(string) ||
                                Nullable.GetUnderlyingType(type)?.IsEnum == true ||
                                Nullable.GetUnderlyingType(type)?.IsPrimitive == true))
        {
            return;
        }

        if (type.IsArray)
        {
            AssertContractType(action, type.GetElementType()!, allowSimpleType: false);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                AssertContractType(action, argument, allowSimpleType: false);
            }

            return;
        }

        var isApiContract = type.Namespace?.StartsWith("Elearning.Api.Contracts", StringComparison.Ordinal) == true;
        Assert.True(
            isApiContract,
            $"Controller action {action.DeclaringType?.Name}.{action.Name} exposes non-API contract type {type.FullName}.");
    }

    private static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Elearning.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the solution root.");
    }

    private static void AssertDoesNotReference(
        System.Reflection.Assembly assembly,
        params string[] forbiddenAssemblyNames)
    {
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
        foreach (var forbiddenName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenName, references);
        }
    }
}
