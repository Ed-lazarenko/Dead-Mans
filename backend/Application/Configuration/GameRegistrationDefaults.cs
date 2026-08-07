using backend.Domain.Persistence;

namespace backend.Application.Configuration;

public static class GameRegistrationDefaults
{
    public const short MinPlayersPerTeam = 1;
    public const short MaxPlayersPerTeam = 2;
    public const int DefaultSlotCount = 6;

    public static IReadOnlyList<(int TeamSlotIndex, string TeamSlotType)> BuildDefaultTeamSlots()
    {
        var teamSlots = new (int TeamSlotIndex, string TeamSlotType)[DefaultSlotCount];
        for (var index = 1; index <= DefaultSlotCount; index += 1)
        {
            teamSlots[index - 1] = (index, TeamSlotTypeValue.Public);
        }

        return teamSlots;
    }
}
