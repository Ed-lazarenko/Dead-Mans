using backend.Api.Contracts;

namespace Backend.Tests.Unit.OpenApi;

public sealed class OpenApiRealtimeContractTests
{
    private static string ReadOpenApiYaml()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var openApiPath = TryFindOpenApiPath(startPath);
            if (openApiPath is not null)
            {
                return File.ReadAllText(openApiPath);
            }
        }

        throw new DirectoryNotFoundException("Could not locate backend/openapi/deadmans.v1.yaml from the test runtime.");
    }

    private static string? TryFindOpenApiPath(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            var openApiPath = Path.Combine(current.FullName, "backend", "openapi", "deadmans.v1.yaml");
            if (File.Exists(openApiPath))
            {
                return openApiPath;
            }

            current = current.Parent;
        }

        return null;
    }

    [Fact]
    public void OpenApiDocumentsGameBoardRealtimeHub()
    {
        var yaml = ReadOpenApiYaml();

        Assert.Contains($"path: {RealtimeHubContracts.GameBoard.HubPath}", yaml, StringComparison.Ordinal);
        Assert.Contains(RealtimeHubContracts.GameBoard.CellOpenedEvent + ":", yaml, StringComparison.Ordinal);
        Assert.Contains("GameCellOpenedEventDto:", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenApiDocumentsGameSetupRealtimeHub()
    {
        var yaml = ReadOpenApiYaml();

        Assert.Contains($"path: {RealtimeHubContracts.GameSetup.HubPath}", yaml, StringComparison.Ordinal);
        Assert.Contains(RealtimeHubContracts.GameSetup.DraftChangedEvent + ":", yaml, StringComparison.Ordinal);
        Assert.Contains("GameSetupDraftChangedEventDto:", yaml, StringComparison.Ordinal);
    }
}
