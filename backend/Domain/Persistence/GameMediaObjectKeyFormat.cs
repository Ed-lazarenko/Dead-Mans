namespace backend.Domain.Persistence;

public static class GameMediaObjectKeyFormat
{
    public static string BuildGameMediaPrefix(string gamesPrefix, Guid gameId)
    {
        return $"{gamesPrefix.Trim('/')}/{gameId}/";
    }

    public static string BuildCardImageKey(
        string gamesPrefix,
        Guid gameId,
        string cardsGroup,
        int rowIndex,
        int colIndex,
        Guid mediaAssetId,
        string fileExtension
    )
    {
        var extension = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
        return $"{BuildGameMediaPrefix(gamesPrefix, gameId)}{cardsGroup.Trim('/')}/{colIndex + 1}-{rowIndex + 1}/{mediaAssetId:N}{extension}";
    }
}
