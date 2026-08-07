using backend.Application.Contracts;
using backend.Application.Features.GameRegistration;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameRegistrationRepositoryTests
{
    [Fact]
    public async Task RejectTeam_ClosesMembershipAndPreservesHistory_SoUserCanCreateNewTeam()
    {
        await using var db = CreateDbContext();
        var gameId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var utc = DateTime.UtcNow;

        db.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "100",
                Login = "player",
                DisplayName = "Player",
                IsActive = true,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc
            }
        );
        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Ready game",
                Status = GameStatusValue.Ready,
                CreatedAtUtc = utc,
                ReadyAtUtc = utc,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 3
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utc
            }
        );
        db.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = true,
                Status = TeamStatusValue.Forming,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc
            }
        );
        db.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = userId,
                JoinedAtUtc = utc
            }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var reject = await service.RejectTeamAsync(adminId, teamId);
        Assert.True(reject.Success);

        var rejectedTeam = await db.GameTeams.SingleAsync(team => team.Id == teamId);
        Assert.Equal(TeamStatusValue.Rejected, rejectedTeam.Status);
        Assert.Equal(adminId, rejectedTeam.RejectedByUserId);
        Assert.NotNull(rejectedTeam.RejectedAtUtc);

        var historicalMember = await db.GameTeamMembers.SingleAsync(member => member.TeamId == teamId);
        Assert.NotNull(historicalMember.LeftAtUtc);

        var create = await service.CreateTeamAsync(userId, recruitmentOpen: true);
        Assert.True(create.Success);
        Assert.NotNull(create.Value);
        Assert.Equal(1, create.Value!.TeamSlotIndex);
    }

    [Fact]
    public async Task RejectTeam_WhenConfirmed_ReturnsTeamNotJoinable()
    {
        await using var db = CreateDbContext();
        var gameId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var utc = DateTime.UtcNow;

        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Ready game",
                Status = GameStatusValue.Ready,
                CreatedAtUtc = utc,
                ReadyAtUtc = utc,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 3
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utc
            }
        );
        db.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc,
                ConfirmedAtUtc = utc,
                ConfirmedByUserId = adminId
            }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var reject = await service.RejectTeamAsync(adminId, teamId);

        Assert.False(reject.Success);
        Assert.Equal(GameRegistrationErrorCode.TeamNotJoinable, reject.Error);
        Assert.Single(await db.GameTeams.Where(team => team.Id == teamId).ToListAsync());
    }

    [Fact]
    public async Task AcceptInvitation_WhenTeamMissing_ReturnsTeamNotFound()
    {
        await using var db = CreateDbContext();
        var gameId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var utc = DateTime.UtcNow;

        db.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "200",
                Login = "invitee",
                DisplayName = "Invitee",
                IsActive = true,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc
            }
        );
        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Ready game",
                Status = GameStatusValue.Ready,
                CreatedAtUtc = utc,
                ReadyAtUtc = utc,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 3
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utc
            }
        );
        db.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = invitationId,
                GameId = gameId,
                SlotId = slotId,
                TeamId = teamId,
                InvitedUserId = userId,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = utc
            }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var accept = await service.AcceptInvitationAsync(userId, invitationId);

        Assert.False(accept.Success);
        Assert.Equal(GameRegistrationErrorCode.TeamNotFound, accept.Error);
    }

    private static GameRegistrationService CreateService(ApplicationDbContext db)
    {
        var reads = new GameRegistrationReadStore(db);
        var persistence = new DbGameRegistrationPersistence(
            db,
            reads,
            NullLogger<DbGameRegistrationPersistence>.Instance
        );
        return new GameRegistrationService(reads, persistence);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
