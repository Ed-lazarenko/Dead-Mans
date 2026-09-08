namespace backend.Domain.Persistence;

public static class GameBoardPersistence
{
    public const int MinRows = 1;
    public const int MaxRows = 20;
    public const int MinColumns = 1;
    public const int MaxColumns = 12;
    public const int MaxLabelLength = 100;

    public const string CheckSqlDimensions =
        "rows BETWEEN 1 AND 20 AND cols BETWEEN 1 AND 12";
}
