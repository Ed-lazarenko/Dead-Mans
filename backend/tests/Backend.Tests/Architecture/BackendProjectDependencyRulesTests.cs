using System.Xml.Linq;
using System.Text.RegularExpressions;
using backend.Messaging;

namespace Backend.Tests.Architecture;

public sealed class BackendProjectDependencyRulesTests
{
    [Fact]
    public void LayeredProjects_ShouldKeepExpectedProjectReferences()
    {
        var backendRoot = ResolveBackendRoot();

        AssertProjectReferences(
            backendRoot,
            "backend.Domain.csproj",
            Array.Empty<string>()
        );
        AssertProjectReferences(
            backendRoot,
            "backend.Application.csproj",
            ["backend.Domain.csproj"]
        );
        AssertProjectReferences(
            backendRoot,
            "backend.Data.csproj",
            ["backend.Domain.csproj"]
        );
        AssertProjectReferences(
            backendRoot,
            "backend.Api.csproj",
            ["backend.Application.csproj", "backend.Domain.csproj"]
        );
        AssertProjectReferences(
            backendRoot,
            "backend.Infrastructure.csproj",
            [
                "backend.Application.csproj",
                "backend.Data.csproj",
                "backend.Domain.csproj"
            ]
        );
        AssertProjectReferences(
            backendRoot,
            "backend.csproj",
            [
                "backend.Api.csproj",
                "backend.Application.csproj",
                "backend.Data.csproj",
                "backend.Infrastructure.csproj"
            ]
        );
    }

    [Fact]
    public void ExecutableHost_ShouldCompileOnlyCompositionRoot()
    {
        var backendRoot = ResolveBackendRoot();
        var document = XDocument.Load(Path.Combine(backendRoot, "backend.csproj"));
        var compileItems = document
            .Descendants("Compile")
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        Assert.Equal(["Program.cs"], compileItems);
    }

    [Fact]
    public void ApplicationProject_ShouldNotDependOnAspNetCoreSharedFramework()
    {
        var backendRoot = ResolveBackendRoot();
        var csprojPath = Path.Combine(backendRoot, "backend.Application.csproj");
        var document = XDocument.Load(csprojPath);

        var frameworkReferences = document
            .Descendants("FrameworkReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        Assert.DoesNotContain("Microsoft.AspNetCore.App", frameworkReferences);
    }

    [Fact]
    public void SourceFiles_ShouldRespectLayerUsingBoundaries()
    {
        var backendRoot = ResolveBackendRoot();

        AssertNoForbiddenUsings(
            Path.Combine(backendRoot, "Domain"),
            ["using backend.Application", "using backend.Data", "using backend.Infrastructure", "using backend.Api"]
        );
        AssertNoForbiddenUsings(
            Path.Combine(backendRoot, "Application"),
            ["using backend.Data", "using backend.Infrastructure", "using backend.Controllers", "using Microsoft.AspNetCore"]
        );
        AssertNoForbiddenUsings(
            Path.Combine(backendRoot, "Data"),
            ["using backend.Application", "using backend.Infrastructure", "using backend.Controllers", "using backend.Api"]
        );
        AssertNoForbiddenUsings(
            Path.Combine(backendRoot, "Api"),
            ["using backend.Infrastructure", "using backend.Data", "using backend.Controllers"]
        );
        AssertNoForbiddenUsings(
            Path.Combine(backendRoot, "Infrastructure"),
            [
                "using backend.Api",
                "using backend.Controllers",
                "using Microsoft.AspNetCore.Cors"
            ]
        );
    }

    [Fact]
    public void ErrorCodeCatalog_ShouldMatchOpenApiEnum()
    {
        var backendRoot = ResolveBackendRoot();
        var openApiPath = Path.Combine(backendRoot, "openapi", "deadmans.v1.yaml");
        var openApiCodes = ExtractOpenApiErrorCodes(openApiPath);

        var catalogCodes = typeof(AppMessages.ErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => field.GetRawConstantValue())
            .OfType<string>()
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(catalogCodes, openApiCodes);
    }

    [Fact]
    public void BackendSource_ShouldNotContainHardcodedErrorCodeLiteralsOutsideCatalog()
    {
        var backendRoot = ResolveBackendRoot();
        var catalogFile = Path.Combine(backendRoot, "Messaging", "AppMessages.cs");
        var hardcodedCodePattern = new Regex(
            "\"game_[a-z0-9_]+\\.[a-z0-9_\\.]+\"",
            RegexOptions.Compiled
        );

        var violations = Directory
            .EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Equals(catalogFile, StringComparison.OrdinalIgnoreCase))
            .Select(path => new
            {
                Path = path,
                Matches = hardcodedCodePattern.Matches(File.ReadAllText(path)).Select(match => match.Value).ToArray()
            })
            .Where(item => item.Matches.Length > 0)
            .Select(item => $"{Path.GetRelativePath(backendRoot, item.Path)} -> {string.Join(", ", item.Matches)}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Found hardcoded error code literals outside AppMessages.ErrorCodes:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void BackendSource_ShouldUseErrorResponseFactoryInsteadOfDirectConstruction()
    {
        var backendRoot = ResolveBackendRoot();
        var factoryFile = Path.Combine(
            backendRoot,
            "Api",
            "Contracts",
            "ErrorResponseFactory.cs"
        );

        var violations = Directory
            .EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Equals(factoryFile, StringComparison.OrdinalIgnoreCase))
            .Select(path => new
            {
                Path = path,
                Lines = File.ReadAllLines(path)
            })
            .SelectMany(
                file =>
                    file.Lines.Select(
                        (line, index) => new
                        {
                            file.Path,
                            Line = line.Trim(),
                            LineNumber = index + 1
                        }
                    )
            )
            .Where(item => item.Line.Contains("new ErrorResponse(", StringComparison.Ordinal))
            .Select(
                item =>
                    $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Found direct ErrorResponse construction outside ErrorResponseFactory:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void Controllers_ShouldUseApiErrorResultHelpersInsteadOfErrorResponseFactory()
    {
        var backendRoot = ResolveBackendRoot();
        var controllersRoot = Path.Combine(backendRoot, "Api", "Controllers");

        var violations = Directory
            .EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Lines = File.ReadAllLines(path)
            })
            .SelectMany(
                file =>
                    file.Lines.Select(
                        (line, index) => new
                        {
                            file.Path,
                            Line = line.Trim(),
                            LineNumber = index + 1
                        }
                    )
            )
            .Where(item => item.Line.Contains("ErrorResponseFactory.Create(", StringComparison.Ordinal))
            .Select(
                item =>
                    $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Controllers must build error IActionResult via ApiErrorResults helpers:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void Controllers_ShouldNotCatchGenericException_ForRequestPipelineErrors()
    {
        var backendRoot = ResolveBackendRoot();
        var controllersRoot = Path.Combine(backendRoot, "Api", "Controllers");

        var violations = Directory
            .EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Lines = File.ReadAllLines(path)
            })
            .SelectMany(
                file =>
                    file.Lines.Select(
                        (line, index) => new
                        {
                            file.Path,
                            Line = line.Trim(),
                            LineNumber = index + 1
                        }
                    )
            )
            .Where(item => item.Line.StartsWith("catch (Exception", StringComparison.Ordinal))
            .Select(
                item =>
                    $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Controllers should rely on ApiExceptionHandlingMiddleware for unhandled exceptions:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void ApiProject_ShouldTreatSwitchExhaustivenessWarningAsError()
    {
        var backendRoot = ResolveBackendRoot();
        var apiCsprojPath = Path.Combine(backendRoot, "backend.Api.csproj");
        var document = XDocument.Load(apiCsprojPath);

        var warningsAsErrors = document
            .Descendants("WarningsAsErrors")
            .Select(element => element.Value)
            .ToArray();

        Assert.Contains(
            warningsAsErrors,
            value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains("CS8509", StringComparer.Ordinal)
        );
    }

    [Fact]
    public void ProductionProjects_ShouldEnforceRecommendedAnalyzersAndWarningsAsErrors()
    {
        var backendRoot = ResolveBackendRoot();
        var propsPath = Path.Combine(backendRoot, "Directory.Build.props");
        var document = XDocument.Load(propsPath);

        Assert.Equal(
            "latest-recommended",
            Assert.Single(document.Descendants("AnalysisLevel")).Value
        );
        Assert.Equal(
            "true",
            Assert.Single(document.Descendants("EnforceCodeStyleInBuild")).Value
        );
        Assert.Equal(
            "true",
            Assert.Single(document.Descendants("MSBuildTreatWarningsAsErrors")).Value
        );
        Assert.Equal(
            "true",
            Assert.Single(document.Descendants("TreatWarningsAsErrors")).Value
        );
    }

    [Fact]
    public void BackendProjects_ShouldUseCentralPackageVersionCatalog()
    {
        var backendRoot = ResolveBackendRoot();
        var catalogPath = Path.Combine(backendRoot, "Directory.Packages.props");
        var catalog = XDocument.Load(catalogPath);

        Assert.Equal(
            "true",
            Assert.Single(catalog.Descendants("ManagePackageVersionsCentrally")).Value
        );

        var violations = Directory
            .EnumerateFiles(backendRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedProjectPath(backendRoot, path))
            .SelectMany(path =>
                XDocument
                    .Load(path)
                    .Descendants("PackageReference")
                    .Where(reference =>
                        reference.Attribute("Version") is not null
                        || reference.Attribute("VersionOverride") is not null
                    )
                    .Select(reference =>
                        $"{Path.GetRelativePath(backendRoot, path)} -> {reference.Attribute("Include")?.Value}"
                    )
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Package versions must be declared only in Directory.Packages.props:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void DomainErrorHttpPolicy_ShouldAvoidDefaultSwitchBranch()
    {
        var backendRoot = ResolveBackendRoot();
        var policyPath = Path.Combine(backendRoot, "Api", "Errors", "DomainErrorHttpPolicy.cs");
        var content = File.ReadAllText(policyPath);

        Assert.DoesNotContain("_ =>", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreAndControllers_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = new[] { "Domain", "Application", Path.Combine("Api", "Controllers") }
            .SelectMany(directory =>
                Directory.EnumerateFiles(
                    Path.Combine(backendRoot, directory),
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Core/application code must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void AuthenticationInfrastructure_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var authRoot = Path.Combine(backendRoot, "Infrastructure", "Auth");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = Directory
            .EnumerateFiles(authRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Authentication infrastructure must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameRegistrationPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceRoot = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = Directory
            .EnumerateFiles(
                persistenceRoot,
                "DbGameRegistrationPersistence*.cs",
                SearchOption.TopDirectoryOnly
            )
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game registration persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameModifierPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceRoot = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = Directory
            .EnumerateFiles(
                persistenceRoot,
                "DbGameModifierRepository*.cs",
                SearchOption.TopDirectoryOnly
            )
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game modifier persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameLifecyclePersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceRoot = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var persistenceFiles = Directory.EnumerateFiles(
            persistenceRoot,
            "DbGameLifecyclePersistence*.cs",
            SearchOption.TopDirectoryOnly
        ).Append(Path.Combine(persistenceRoot, "GameTeamSlotInitializer.cs"));
        var violations = persistenceFiles
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game lifecycle persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameQuestionPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var path = Path.Combine(
            backendRoot,
            "Infrastructure",
            "Persistence",
            "DbGameQuestionRepository.cs"
        );
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = File.ReadAllLines(path)
            .Select(
                (line, index) => new
                {
                    Line = line.Trim(),
                    LineNumber = index + 1
                }
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game question persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameBoardPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var path = Path.Combine(
            backendRoot,
            "Infrastructure",
            "Persistence",
            "DbGameBoardRepository.cs"
        );
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = File.ReadAllLines(path)
            .Select(
                (line, index) => new
                {
                    Line = line.Trim(),
                    LineNumber = index + 1
                }
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game board persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameRoundPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceRoot = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = Directory
            .EnumerateFiles(
                persistenceRoot,
                "DbGameRoundRepository*.cs",
                SearchOption.TopDirectoryOnly
            )
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game round persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameSetupPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceRoot = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var paths = new[]
        {
            Path.Combine(persistenceRoot, "DbGameSetupRepository.cs"),
            Path.Combine(persistenceRoot, "DbGameSetupCellMediaRepository.cs")
        };
        var violations = paths
            .SelectMany(path =>
                File.ReadAllLines(path).Select(
                    (line, index) => new
                    {
                        Path = path,
                        Line = line.Trim(),
                        LineNumber = index + 1
                    }
                )
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, item.Path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game setup persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameNotificationPersistence_ShouldUseInjectedClock()
    {
        var backendRoot = ResolveBackendRoot();
        var path = Path.Combine(
            backendRoot,
            "Infrastructure",
            "Persistence",
            "DbGameNotificationRepository.cs"
        );
        var forbiddenClockAccess = new[]
        {
            "DateTime.UtcNow",
            "DateTime.Now",
            "DateTimeOffset.UtcNow",
            "DateTimeOffset.Now"
        };
        var violations = File.ReadAllLines(path)
            .Select(
                (line, index) => new
                {
                    Line = line.Trim(),
                    LineNumber = index + 1
                }
            )
            .Where(item =>
                forbiddenClockAccess.Any(value =>
                    item.Line.Contains(value, StringComparison.Ordinal)
                )
            )
            .Select(item =>
                $"{Path.GetRelativePath(backendRoot, path)}:{item.LineNumber} -> {item.Line}"
            )
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game notification persistence must use injected TimeProvider:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations)
        );
    }

    [Fact]
    public void GameRegistrationService_ShouldKeepUseCasesSeparated()
    {
        var backendRoot = ResolveBackendRoot();
        var featureDirectory = Path.Combine(
            backendRoot,
            "Application",
            "Features",
            "GameRegistration"
        );
        var expectedFiles = new[]
        {
            "GameRegistrationService.cs",
            "GameRegistrationService.AdminTeams.cs",
            "GameRegistrationService.Invitations.cs",
            "GameRegistrationService.Queries.cs"
        };

        var actualFiles = Directory
            .EnumerateFiles(featureDirectory, "GameRegistrationService*.cs")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFiles.OrderBy(name => name, StringComparer.Ordinal), actualFiles);
        foreach (var fileName in expectedFiles)
        {
            var lineCount = File.ReadLines(Path.Combine(featureDirectory, fileName)).Count();
            Assert.True(lineCount <= 450, $"{fileName} grew to {lineCount} lines; split its use cases before adding more behavior.");
        }
    }

    [Fact]
    public void GameRegistrationReadStore_ShouldKeepQueriesSeparated()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceDirectory = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var expectedFiles = new[]
        {
            "GameRegistrationReadStore.cs",
            "GameRegistrationReadStore.Snapshots.cs",
            "GameRegistrationReadStore.TeamProjections.cs"
        };

        var actualFiles = Directory
            .EnumerateFiles(persistenceDirectory, "GameRegistrationReadStore*.cs")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFiles.OrderBy(name => name, StringComparer.Ordinal), actualFiles);
        foreach (var fileName in expectedFiles)
        {
            var lineCount = File.ReadLines(Path.Combine(persistenceDirectory, fileName)).Count();
            Assert.True(lineCount <= 400, $"{fileName} grew to {lineCount} lines; split its query responsibility before adding more behavior.");
        }
    }

    [Fact]
    public void GameQuestionRepository_ShouldKeepCatalogResponsibilitiesSeparated()
    {
        var backendRoot = ResolveBackendRoot();
        var persistenceDirectory = Path.Combine(backendRoot, "Infrastructure", "Persistence");
        var expectedFiles = new[]
        {
            "DbGameQuestionRepository.cs",
            "DbGameQuestionRepository.Catalog.cs",
            "DbGameQuestionRepository.Categories.cs",
            "DbGameQuestionRepository.Import.cs",
            "DbGameQuestionRepository.Questions.cs"
        };

        var actualFiles = Directory
            .EnumerateFiles(persistenceDirectory, "DbGameQuestionRepository*.cs")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFiles.OrderBy(name => name, StringComparer.Ordinal), actualFiles);
        foreach (var fileName in expectedFiles)
        {
            var lineCount = File.ReadLines(Path.Combine(persistenceDirectory, fileName)).Count();
            Assert.True(lineCount <= 350, $"{fileName} grew to {lineCount} lines; split its catalog responsibility before adding more behavior.");
        }
    }

    private static void AssertProjectReferences(
        string backendRoot,
        string projectName,
        IReadOnlyCollection<string> expectedReferences
    )
    {
        var csprojPath = Path.Combine(backendRoot, projectName);
        var document = XDocument.Load(csprojPath);
        var actualReferences = document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Path.GetFileName)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var expected = expectedReferences.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, actualReferences);
    }

    private static bool IsGeneratedProjectPath(string backendRoot, string path)
    {
        var relativePath = Path.GetRelativePath(backendRoot, path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || segment.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string ResolveBackendRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "backend", "backend.slnx");
            if (File.Exists(candidate))
            {
                return Path.Combine(current.FullName, "backend");
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to resolve backend root from test output directory.");
    }

    private static void AssertNoForbiddenUsings(
        string directory,
        IReadOnlyCollection<string> forbiddenUsingPrefixes
    )
    {
        var violations = Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Lines = File.ReadAllLines(path)
            })
            .SelectMany(
                file => file.Lines.Select((line, index) => new { file.Path, Line = line.Trim(), LineNumber = index + 1 })
            )
            .Where(item => forbiddenUsingPrefixes.Any(prefix => item.Line.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(item => $"{Path.GetRelativePath(directory, item.Path)}:{item.LineNumber} -> {item.Line}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Found forbidden usings in '{Path.GetFileName(directory)}':{Environment.NewLine}{string.Join(Environment.NewLine, violations)}"
        );
    }

    private static string[] ExtractOpenApiErrorCodes(string openApiPath)
    {
        var lines = File.ReadAllLines(openApiPath);
        var schemaIndex = Array.FindIndex(lines, line => line.Trim() == "ErrorResponse:");
        Assert.True(schemaIndex >= 0, "OpenAPI schema 'ErrorResponse' was not found.");

        var codeIndex = -1;
        for (var i = schemaIndex + 1; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            if (lines[i].Trim() == "code:")
            {
                codeIndex = i;
                break;
            }
        }

        Assert.True(codeIndex >= 0, "OpenAPI 'ErrorResponse.code' property was not found.");

        var enumIndex = -1;
        for (var i = codeIndex + 1; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("      ", StringComparison.Ordinal))
            {
                break;
            }

            if (lines[i].Trim() == "enum:")
            {
                enumIndex = i;
                break;
            }
        }

        Assert.True(enumIndex >= 0, "OpenAPI 'ErrorResponse.code.enum' section was not found.");

        var values = new List<string>();
        for (var i = enumIndex + 1; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("            - ", StringComparison.Ordinal))
            {
                break;
            }

            values.Add(lines[i].Trim().Substring(2).Trim());
        }

        return values.OrderBy(code => code, StringComparer.Ordinal).ToArray();
    }
}
