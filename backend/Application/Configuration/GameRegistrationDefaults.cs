using backend.Domain.Persistence;

namespace backend.Application.Configuration;

public static class GameRegistrationDefaults
{
    public const short MinPlayersPerTeam = 1;
    public const short MaxPlayersPerTeam = 2;
    public const int DefaultSlotCount = 6;

    public static IReadOnlyList<(int SlotIndex, string Availability)> BuildDefaultSlots()
    {
        var slots = new (int, string)[DefaultSlotCount];
        for (var index = 1; index <= DefaultSlotCount; index += 1)
        {
            slots[index - 1] = (index, SlotAvailabilityValue.Public);
        }

        return slots;
    }
}
