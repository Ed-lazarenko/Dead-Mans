using backend.Application.Abstractions.Repositories;
using backend.Application.Configuration;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameSetup;

internal static class GameSetupCellMediaValidator
{
    private const int MaximumSignatureLength = 12;
    private static ReadOnlySpan<byte> JpegSignature => [0xff, 0xd8, 0xff];
    private static ReadOnlySpan<byte> PngSignature =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static bool IsAllowedUpload(string? contentType, long length, out string normalizedMimeType)
    {
        normalizedMimeType = string.Empty;
        if (length <= 0 || length > GameSetupCellMediaLimits.MaxUploadBytes)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType)
            || !GameSetupCellMediaLimits.AllowedMimeTypes.Contains(contentType.Trim()))
        {
            return false;
        }

        normalizedMimeType = contentType.Trim();
        return true;
    }

    public static bool HasMatchingFileSignature(Stream content, string mimeType)
    {
        if (!content.CanRead || !content.CanSeek)
        {
            return false;
        }

        var initialPosition = content.Position;
        Span<byte> header = stackalloc byte[MaximumSignatureLength];
        var bytesRead = 0;

        try
        {
            while (bytesRead < header.Length)
            {
                var read = content.Read(header[bytesRead..]);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }
        }
        finally
        {
            content.Position = initialPosition;
        }

        var availableHeader = header[..bytesRead];
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => availableHeader.StartsWith(JpegSignature),
            "image/png" => availableHeader.StartsWith(PngSignature),
            "image/gif" => availableHeader.StartsWith("GIF87a"u8)
                || availableHeader.StartsWith("GIF89a"u8),
            "image/webp" => availableHeader.Length >= 12
                && availableHeader[..4].SequenceEqual("RIFF"u8)
                && availableHeader[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };
    }

    public static string BuildObjectKey(
        MediaStorageSettings storageSettings,
        GameSetupDraftCellRef draftCell,
        Guid mediaAssetId,
        string extension
    )
    {
        return GameMediaObjectKeyFormat.BuildCardImageKey(
            storageSettings.GamesPrefix,
            draftCell.GameId,
            storageSettings.CardsGroup,
            draftCell.RowIndex,
            draftCell.ColIndex,
            mediaAssetId,
            extension
        );
    }

    public static string ResolveExtension(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => string.Empty,
        };
    }
}
