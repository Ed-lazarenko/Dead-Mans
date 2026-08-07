using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameRegistrationContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameRegistrationContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRegistration_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/game/registration");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegistration_WhenNoReadyGame_ReturnsNotFound()
    {
        await ClearRegistrationDataAsync();
        using var viewerClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await viewerClient.GetAsync("/api/game/registration");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationNotOpen, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationNotOpen, payload.Code);
    }

    [Fact]
    public async Task ListTeams_WhenAdminAndNoReadyGame_ReturnsNotFound()
    {
        await ClearRegistrationDataAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/registration/teams");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationNotOpen, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationNotOpen, payload.Code);
    }

    [Fact]
    public async Task GetAdminSnapshot_WhenReadyGame_ReturnsAvailablePlayersAndLimits()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var assignedUserId = Guid.NewGuid();
        var availableUserId = Guid.NewGuid();
        var pendingInviteUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            assignedUserId,
            recruitmentOpen: true,
            slotIndex: 2,
            memberUserIds: [assignedUserId]
        );
        await SeedUserAsync(availableUserId, "available-player");
        await SeedUserAsync(pendingInviteUserId, "pending-player");
        var pendingInviteSlotId = await CreateSlotAsync(3);
        await SeedPendingInvitationForReadyGameAsync(pendingInviteUserId, pendingInviteSlotId);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/registration/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.MinPlayersPerTeam);
        Assert.Equal(2, payload.MaxPlayersPerTeam);
        Assert.Contains(payload.Teams, team => team.TeamId == teamId);
        Assert.Contains(payload.AvailablePlayers, player => player.UserId == availableUserId);
        Assert.DoesNotContain(payload.AvailablePlayers, player => player.UserId == assignedUserId);
        Assert.DoesNotContain(payload.AvailablePlayers, player => player.UserId == pendingInviteUserId);
    }

    [Fact]
    public async Task GetAdminSnapshot_WhenModerator_ReturnsAvailablePlayersAndLimits()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var availableUserId = Guid.NewGuid();
        await SeedUserAsync(availableUserId, "available-player");
        using var moderatorClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator]
        );

        var response = await moderatorClient.GetAsync("/api/game/registration/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Contains(payload.AvailablePlayers, player => player.UserId == availableUserId);
    }

    [Fact]
    public async Task CreateTeam_WhenReadyGame_ReturnsCreated()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        await SeedReadyGameAsync();
        await SeedUserAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var response = await viewerClient.PostAsJsonAsync(
            "/api/game/registration/teams",
            new CreateRegistrationTeamRequestDto(true)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal("forming", payload.Status);
    }

    [Fact]
    public async Task CreateTeam_WhenUserAlreadyHasActiveTeam_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        await SeedReadyGameAsync();
        await SeedUserAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var firstResponse = await viewerClient.PostAsJsonAsync(
            "/api/game/registration/teams",
            new CreateRegistrationTeamRequestDto(true)
        );
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await viewerClient.PostAsJsonAsync(
            "/api/game/registration/teams",
            new CreateRegistrationTeamRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationAlreadyOnTeam, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationAlreadyOnTeam, payload.Code);
    }

    [Fact]
    public async Task CreateAdminTeam_WhenSlotProvided_ReturnsCreatedOnRequestedSlot()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var slotId = await CreateSlotAsync(2);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/registration/admin/teams",
            new CreateAdminRegistrationTeamRequestDto(slotId, false)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.SlotIndex);
        Assert.False(payload.RecruitmentOpen);
        Assert.Empty(payload.Members);
        Assert.Equal("forming", payload.Status);
    }

    [Fact]
    public async Task CreatePlayerInvitation_WhenOwnerHasClosedTeam_ReturnsCreatedAndAppearsForInvitee()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        using var ownerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            ownerId
        );
        using var invitedClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            invitedUserId
        );

        var response = await ownerClient.PostAsJsonAsync(
            "/api/game/registration/my-team/invitations",
            new CreatePlayerInvitationRequestDto(invitedUserId)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationInvitationDto>();
        Assert.NotNull(payload);
        Assert.Equal(teamId, payload.TeamId);
        Assert.Equal(2, payload.SlotIndex);

        var inviteeSnapshotResponse = await invitedClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, inviteeSnapshotResponse.StatusCode);
        var inviteeSnapshot =
            await inviteeSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(inviteeSnapshot);
        var pendingInvitation = Assert.Single(inviteeSnapshot.MyPendingInvitations);
        Assert.Equal(teamId, pendingInvitation.TeamId);
        Assert.Equal("player", pendingInvitation.InvitedByDisplayName);
        Assert.Equal("invited-player", pendingInvitation.InvitedUserDisplayName);

        var ownerSnapshotResponse = await ownerClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, ownerSnapshotResponse.StatusCode);
        var ownerSnapshot =
            await ownerSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(ownerSnapshot);
        Assert.False(ownerSnapshot.CanInvitePlayersToMyTeam);
        Assert.Single(ownerSnapshot.MyOutgoingInvitations);
        Assert.NotNull(ownerSnapshot.MyTeam);
        var ownerPendingInvitation = Assert.Single(ownerSnapshot.MyTeam.PendingInvitations);
        Assert.Equal(payload.InvitationId, ownerPendingInvitation.InvitationId);
        Assert.Equal(invitedUserId, ownerPendingInvitation.Player.UserId);
        Assert.Equal("invited-player", ownerPendingInvitation.Player.DisplayName);

        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var adminSnapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, adminSnapshotResponse.StatusCode);
        var adminSnapshot = await adminSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(adminSnapshot);
        var adminTeam = Assert.Single(adminSnapshot.Teams, team => team.TeamId == teamId);
        var adminPendingInvitation = Assert.Single(adminTeam.PendingInvitations);
        Assert.Equal(invitedUserId, adminPendingInvitation.Player.UserId);
    }

    [Fact]
    public async Task CreateAdminInvitation_WhenClosedTeamHasCapacity_AllowsMultiplePendingInvites()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var adminId = Guid.NewGuid();
        var firstInvitedUserId = Guid.NewGuid();
        var secondInvitedUserId = Guid.NewGuid();
        var extraInvitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            adminId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: []
        );
        var slotId = await GetSlotIdByIndexAsync(2);
        await SeedUserAsync(firstInvitedUserId, "first-invited");
        await SeedUserAsync(secondInvitedUserId, "second-invited");
        await SeedUserAsync(extraInvitedUserId, "extra-invited");
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var firstResponse = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, firstInvitedUserId, teamId)
        );
        var secondResponse = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, secondInvitedUserId, teamId)
        );
        var extraResponse = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, extraInvitedUserId, teamId)
        );

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, extraResponse.StatusCode);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var team = Assert.Single(snapshot.Teams, candidate => candidate.TeamId == teamId);
        Assert.Equal(2, team.PendingInvitations.Count);
        Assert.Contains(team.PendingInvitations, invitation =>
            invitation.Player.UserId == firstInvitedUserId
        );
        Assert.Contains(team.PendingInvitations, invitation =>
            invitation.Player.UserId == secondInvitedUserId
        );
    }

    [Fact]
    public async Task CreateAdminInvitation_WhenGameActiveAndClosedTeamHasCapacity_ReturnsCreated()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var adminId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            adminId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: []
        );
        var slotId = await GetSlotIdByIndexAsync(2);
        await SeedUserAsync(invitedUserId, "active-invited");
        await SetReadyGameActiveAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, invitedUserId, teamId)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationInvitationDto>();
        Assert.NotNull(payload);
        Assert.Equal(teamId, payload.TeamId);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(GameStatusValue.Active, snapshot.GameStatus);
        var team = Assert.Single(snapshot.Teams, candidate => candidate.TeamId == teamId);
        var invitation = Assert.Single(team.PendingInvitations);
        Assert.Equal(invitedUserId, invitation.Player.UserId);
    }

    [Fact]
    public async Task CreateAdminInvitation_WhenTeamRecruitmentOpen_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var adminId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            adminId,
            recruitmentOpen: true,
            slotIndex: 2,
            memberUserIds: [adminId]
        );
        var slotId = await GetSlotIdByIndexAsync(2);
        await SeedUserAsync(invitedUserId, "invited-player");
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, invitedUserId, teamId)
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamInviteNotAllowed, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamInviteNotAllowed, payload.Code);
    }

    [Fact]
    public async Task AssignPlayer_WhenPlayerIsMovedFromConfirmedTeam_DemotesSourceAndAddsMemberToTarget()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var firstUserId = Guid.NewGuid();
        var movedUserId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var sourceTeamId = await SeedTeamAsync(
            firstUserId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [firstUserId, movedUserId],
            status: TeamStatusValue.Confirmed
        );
        var targetTeamId = await SeedTeamAsync(
            targetOwnerId,
            recruitmentOpen: true,
            slotIndex: 3,
            memberUserIds: [targetOwnerId]
        );
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/registration/admin/teams/{targetTeamId}/assign",
            new AssignRegistrationPlayerRequestDto(movedUserId)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal(targetTeamId, payload.TeamId);
        Assert.Equal(2, payload.Members.Count);
        Assert.Contains(payload.Members, member => member.Player.UserId == movedUserId);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var sourceTeam = Assert.Single(snapshot.Teams, team => team.TeamId == sourceTeamId);
        Assert.Equal("forming", sourceTeam.Status);
        Assert.Single(sourceTeam.Members);
        Assert.DoesNotContain(sourceTeam.Members, member => member.Player.UserId == movedUserId);
    }

    [Fact]
    public async Task AssignPlayer_WhenLastMemberLeavesSourceTeam_DisbandsSourceAndCancelsPendingInvitations()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var sourceOwnerId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var sourceTeamId = await SeedTeamAsync(
            sourceOwnerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [sourceOwnerId]
        );
        var targetTeamId = await SeedTeamAsync(
            targetOwnerId,
            recruitmentOpen: true,
            slotIndex: 3,
            memberUserIds: [targetOwnerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        await SeedPlayerInvitationAsync(sourceTeamId, sourceOwnerId, invitedUserId);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        using var invitedClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            invitedUserId
        );

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/registration/admin/teams/{targetTeamId}/assign",
            new AssignRegistrationPlayerRequestDto(sourceOwnerId)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(snapshot.Teams, team => team.TeamId == sourceTeamId);
        var targetTeam = Assert.Single(snapshot.Teams, team => team.TeamId == targetTeamId);
        Assert.Equal(2, targetTeam.Members.Count);
        Assert.Contains(targetTeam.Members, member => member.Player.UserId == sourceOwnerId);

        var inviteeSnapshotResponse = await invitedClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, inviteeSnapshotResponse.StatusCode);
        var inviteeSnapshot =
            await inviteeSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(inviteeSnapshot);
        Assert.Empty(inviteeSnapshot.MyPendingInvitations);
    }

    [Fact]
    public async Task AssignPlayer_WhenTargetTeamIsFull_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var extraPlayerId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: true,
            slotIndex: 2,
            memberUserIds: [ownerId, teammateId]
        );
        await SeedUserAsync(extraPlayerId, "extra-player");
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/registration/admin/teams/{teamId}/assign",
            new AssignRegistrationPlayerRequestDto(extraPlayerId)
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamNotJoinable, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable, payload.Code);
    }

    [Fact]
    public async Task RemovePlayerFromTeam_WhenAdmin_RemovesMemberAndDemotesConfirmedTeam()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var removedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId, removedUserId],
            status: TeamStatusValue.Confirmed
        );
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync(
            $"/api/game/registration/admin/teams/{teamId}/members/{removedUserId}/remove",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var team = Assert.Single(snapshot.Teams, candidate => candidate.TeamId == teamId);
        Assert.Equal("forming", team.Status);
        Assert.Single(team.Members);
        Assert.Contains(team.Members, member => member.Player.UserId == ownerId);
        Assert.DoesNotContain(team.Members, member => member.Player.UserId == removedUserId);
        Assert.Contains(snapshot.AvailablePlayers, player => player.UserId == removedUserId);
    }

    [Fact]
    public async Task CancelTeamInvitation_WhenAdmin_RemovesPendingInvitationFromRosterAndInvitee()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        var invitationId = await SeedPlayerInvitationAsync(teamId, ownerId, invitedUserId);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        using var invitedClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            invitedUserId
        );

        var response = await adminClient.PostAsync(
            $"/api/game/registration/admin/teams/{teamId}/invitations/{invitationId}/cancel",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var team = Assert.Single(snapshot.Teams, candidate => candidate.TeamId == teamId);
        Assert.Empty(team.PendingInvitations);

        var inviteeSnapshotResponse = await invitedClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, inviteeSnapshotResponse.StatusCode);
        var inviteeSnapshot =
            await inviteeSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(inviteeSnapshot);
        Assert.Empty(inviteeSnapshot.MyPendingInvitations);
    }

    [Fact]
    public async Task MoveTeam_WhenTargetSlotOccupied_SwapsTeamsBetweenSlots()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var firstOwnerId = Guid.NewGuid();
        var secondOwnerId = Guid.NewGuid();
        var firstTeamId = await SeedTeamAsync(
            firstOwnerId,
            recruitmentOpen: true,
            slotIndex: 2,
            memberUserIds: [firstOwnerId]
        );
        var secondTeamId = await SeedTeamAsync(
            secondOwnerId,
            recruitmentOpen: false,
            slotIndex: 3,
            memberUserIds: [secondOwnerId]
        );
        var targetSlotId = await GetSlotIdByIndexAsync(3);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/registration/admin/teams/{firstTeamId}/move",
            new MoveRegistrationTeamRequestDto(targetSlotId)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.SlotIndex);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var firstTeam = Assert.Single(snapshot.Teams, team => team.TeamId == firstTeamId);
        var secondTeam = Assert.Single(snapshot.Teams, team => team.TeamId == secondTeamId);
        Assert.Equal(3, firstTeam.SlotIndex);
        Assert.Equal(2, secondTeam.SlotIndex);
    }

    [Fact]
    public async Task RejectTeam_WhenConfirmed_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var adminId = Guid.NewGuid();
        var teamId = await SeedConfirmedTeamAsync(adminId);
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/reject",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamNotJoinable, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable, payload.Code);
    }

    [Fact]
    public async Task LeaveTeam_WhenMember_ReturnsOk()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        await SeedReadyGameAsync();
        await SeedUserAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var createResponse = await viewerClient.PostAsJsonAsync(
            "/api/game/registration/teams",
            new CreateRegistrationTeamRequestDto(true)
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await viewerClient.PostAsync("/api/game/registration/teams/leave", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task LeaveTeam_WhenOutgoingPlayerInvitationExists_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        await SeedPlayerInvitationAsync(teamId, ownerId, invitedUserId);
        using var ownerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            ownerId
        );

        var response = await ownerClient.PostAsync("/api/game/registration/teams/leave", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationPendingOutgoingInvitation, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationPendingOutgoingInvitation, payload.Code);
    }

    [Fact]
    public async Task LeaveTeam_WhenTeamConfirmed_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        var teamId = await SeedConfirmedTeamAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var response = await viewerClient.PostAsync("/api/game/registration/teams/leave", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamNotJoinable, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable, payload.Code);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(
            await dbContext.GameTeamMembers.AnyAsync(member =>
                member.TeamId == teamId && member.UserId == userId && member.LeftAtUtc == null
            )
        );
    }

    [Fact]
    public async Task RequestMyTeamDisband_WhenConfirmedMember_ReturnsTeamAndMarksRequest()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        var teamId = await SeedConfirmedTeamAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await viewerClient.PostAsync(
            "/api/game/registration/my-team/disband-request",
            content: null
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal(teamId, payload.TeamId);
        Assert.Equal("confirmed", payload.Status);
        Assert.Equal(userId, payload.DisbandRequestedByUserId);
        Assert.Equal("player", payload.DisbandRequestedByDisplayName);
        Assert.NotNull(payload.DisbandRequestedAtUtc);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var team = Assert.Single(snapshot.Teams, item => item.TeamId == teamId);
        Assert.Equal(userId, team.DisbandRequestedByUserId);
        Assert.Equal("player", team.DisbandRequestedByDisplayName);
        Assert.NotNull(team.DisbandRequestedAtUtc);
    }

    [Fact]
    public async Task RequestMyTeamDisband_WhenTeamNotConfirmed_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        var teamId = await SeedFormingTeamAsync(userId, recruitmentOpen: false);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var response = await viewerClient.PostAsync(
            "/api/game/registration/my-team/disband-request",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamNotJoinable, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable, payload.Code);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(item => item.Id == teamId);
        Assert.Null(team.DisbandRequestedAtUtc);
        Assert.Null(team.DisbandRequestedByUserId);
    }

    [Fact]
    public async Task DisbandConfirmedTeam_WhenConfirmedTeam_ReturnsNoContentAndClosesTeam()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var adminId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId, teammateId],
            status: TeamStatusValue.Confirmed
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        var invitationId = await SeedPlayerInvitationAsync(teamId, ownerId, invitedUserId);
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/disband",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbTeam = await dbContext.GameTeams.FirstAsync(team => team.Id == teamId);
        Assert.Equal(TeamStatusValue.Disbanded, dbTeam.Status);
        Assert.Equal(adminId, dbTeam.DisbandedByUserId);
        Assert.NotNull(dbTeam.DisbandedAtUtc);
        Assert.False(
            await dbContext.GameTeamMembers.AnyAsync(member =>
                member.TeamId == teamId && member.LeftAtUtc == null
            )
        );
        var invitation = await dbContext.GameTeamInvitations.FirstAsync(item =>
            item.Id == invitationId
        );
        Assert.Equal(TeamInvitationStatusValue.Cancelled, invitation.Status);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(snapshot.Teams, team => team.TeamId == teamId);
        Assert.Contains(snapshot.AvailablePlayers, player => player.UserId == ownerId);
        Assert.Contains(snapshot.AvailablePlayers, player => player.UserId == teammateId);
    }

    [Fact]
    public async Task DisbandConfirmedTeam_WhenTeamIsActiveInGame_ReturnsConflictAndKeepsTeam()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var adminId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            adminId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [adminId],
            status: TeamStatusValue.Confirmed
        );
        await SetReadyGameActiveAsync(teamId);
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/disband",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamActiveInGame, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamActiveInGame, payload.Code);

        var snapshotResponse = await adminClient.GetAsync("/api/game/registration/admin");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot =
            await snapshotResponse.Content.ReadFromJsonAsync<GameRegistrationAdminSnapshotDto>();
        Assert.NotNull(snapshot);
        var team = Assert.Single(snapshot.Teams, candidate => candidate.TeamId == teamId);
        Assert.True(team.IsActiveInGame);
        Assert.Equal(TeamStatusValue.Confirmed, team.Status);
    }

    [Fact]
    public async Task CancelPlayerInvitation_WhenPending_RemovesOutgoingAndIncomingInvitation()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        var invitationId = await SeedPlayerInvitationAsync(teamId, ownerId, invitedUserId);
        using var ownerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            ownerId
        );
        using var invitedClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            invitedUserId
        );

        var response = await ownerClient.PostAsync(
            $"/api/game/registration/my-team/invitations/{invitationId}/cancel",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var ownerSnapshotResponse = await ownerClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, ownerSnapshotResponse.StatusCode);
        var ownerSnapshot =
            await ownerSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(ownerSnapshot);
        Assert.True(ownerSnapshot.CanInvitePlayersToMyTeam);
        Assert.Empty(ownerSnapshot.MyOutgoingInvitations);

        var inviteeSnapshotResponse = await invitedClient.GetAsync("/api/game/registration");
        Assert.Equal(HttpStatusCode.OK, inviteeSnapshotResponse.StatusCode);
        var inviteeSnapshot =
            await inviteeSnapshotResponse.Content.ReadFromJsonAsync<GameRegistrationSnapshotDto>();
        Assert.NotNull(inviteeSnapshot);
        Assert.Empty(inviteeSnapshot.MyPendingInvitations);
    }

    [Fact]
    public async Task ConfirmTeam_WhenFormingTeamMeetsMinimum_ReturnsOk()
    {
        await ClearRegistrationDataAsync();
        var adminId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var teamId = await SeedFormingTeamAsync(playerId, recruitmentOpen: false);
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/confirm",
            content: null
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegistrationTeamDto>();
        Assert.NotNull(payload);
        Assert.Equal("confirmed", payload.Status);
    }

    [Fact]
    public async Task ConfirmTeam_WhenPendingTeamInvitationExists_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        await SeedReadyGameAsync();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var teamId = await SeedTeamAsync(
            ownerId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
        await SeedUserAsync(invitedUserId, "invited-player");
        await SeedPlayerInvitationAsync(teamId, ownerId, invitedUserId);
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/confirm",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationPendingOutgoingInvitation, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationPendingOutgoingInvitation, payload.Code);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(candidate => candidate.Id == teamId);
        Assert.Equal(TeamStatusValue.Forming, team.Status);
        Assert.Null(team.ConfirmedAtUtc);
    }

    [Fact]
    public async Task DeclineInvitation_WhenPending_ReturnsOk()
    {
        await ClearRegistrationDataAsync();
        var userId = Guid.NewGuid();
        var invitationId = await SeedPendingInvitationAsync(userId);
        await SeedUserAsync(userId);
        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId
        );

        var response = await viewerClient.PostAsync(
            $"/api/game/registration/invitations/{invitationId}/decline",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task JoinTeam_WhenRecruitmentClosed_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var ownerId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var teamId = await SeedFormingTeamAsync(ownerId, recruitmentOpen: false);
        await SeedUserAsync(joinerId, "joiner");
        using var joinerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            joinerId
        );

        var response = await joinerClient.PostAsync(
            $"/api/game/registration/teams/{teamId}/join",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamNotJoinable, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable, payload.Code);
    }

    [Fact]
    public async Task CreateInvitation_WhenDuplicatePendingForUser_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var adminId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        await SeedReadyGameAsync();
        await SeedUserAsync(invitedUserId, "invited-player");
        var teamId = await SeedTeamAsync(
            adminId,
            recruitmentOpen: false,
            slotIndex: 2,
            memberUserIds: []
        );
        var slotId = await GetSlotIdByIndexAsync(2);
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var request = new CreateAdminInvitationRequestDto(slotId, invitedUserId, teamId);

        var firstResponse = await adminClient.PostAsJsonAsync("/api/game/registration/invitations", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await adminClient.PostAsJsonAsync("/api/game/registration/invitations", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationPendingInvitationExists, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationPendingInvitation, payload.Code);
    }

    [Fact]
    public async Task CreateInvitation_WhenInvitedUserMissing_ReturnsNotFoundWithSpecificCode()
    {
        await ClearRegistrationDataAsync();
        var adminId = Guid.NewGuid();
        await SeedReadyGameAsync();
        var slotId = await GetFirstSlotIdForReadyGameAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, Guid.NewGuid(), null)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UserMissingOrInactive, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationUserNotFound, payload.Code);
    }

    [Fact]
    public async Task CreateInvitation_WhenTeamIdMissing_ReturnsConflict()
    {
        await ClearRegistrationDataAsync();
        var adminId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        await SeedReadyGameAsync();
        await SeedUserAsync(invitedUserId, "invited-player");
        var slotId = await GetFirstSlotIdForReadyGameAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/registration/invitations",
            new CreateAdminInvitationRequestDto(slotId, invitedUserId, null)
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRegistrationTeamInviteNotAllowed, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRegistrationTeamInviteNotAllowed, payload.Code);
    }

    private async Task ClearRegistrationDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamInvitations.RemoveRange(dbContext.GameTeamInvitations);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.BoardCellMedia.RemoveRange(dbContext.BoardCellMedia);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReadyGameAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var utc = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Ready game",
                Status = GameStatusValue.Ready,
                CreatedAtUtc = utc,
                ReadyAtUtc = utc,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 2
            }
        );
        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SlotIndex = 1,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utc
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedUserAsync(Guid userId, string login = "player")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var utc = DateTime.UtcNow;
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = userId.ToString("N")[..15],
                Login = login,
                DisplayName = login,
                IsActive = true,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> CreateSlotAsync(int slotIndex, string availability = SlotAvailabilityValue.Public)
    {
        var gameId = await GetReadyGameIdAsync();
        var slotId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = slotIndex,
                Availability = availability,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
        return slotId;
    }

    private async Task<Guid> SeedTeamAsync(
        Guid createdByUserId,
        bool recruitmentOpen,
        int slotIndex,
        IReadOnlyList<Guid> memberUserIds,
        string status = TeamStatusValue.Forming
    )
    {
        await SeedUserAsync(createdByUserId);
        foreach (var memberUserId in memberUserIds.Where(memberUserId => memberUserId != createdByUserId))
        {
            await SeedUserAsync(memberUserId, $"player-{memberUserId.ToString("N")[..8]}");
        }

        var gameId = await GetReadyGameIdAsync();
        var slotId = await CreateSlotAsync(slotIndex);
        var teamId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = recruitmentOpen,
                Status = status,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc,
                ConfirmedAtUtc = status == TeamStatusValue.Confirmed ? utc : null,
                ConfirmedByUserId = status == TeamStatusValue.Confirmed ? createdByUserId : null
            }
        );
        foreach (var memberUserId in memberUserIds)
        {
            dbContext.GameTeamMembers.Add(
                new GameTeamMember
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    TeamId = teamId,
                    UserId = memberUserId,
                    JoinedAtUtc = utc
                }
            );
        }

        await dbContext.SaveChangesAsync();
        return teamId;
    }

    private async Task<Guid> SeedFormingTeamAsync(Guid ownerId, bool recruitmentOpen)
    {
        await SeedReadyGameAsync();
        return await SeedTeamAsync(
            ownerId,
            recruitmentOpen,
            slotIndex: 2,
            memberUserIds: [ownerId]
        );
    }

    private async Task<Guid> SeedConfirmedTeamAsync(Guid adminId)
    {
        var teamId = await SeedFormingTeamAsync(adminId, recruitmentOpen: false);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(team => team.Id == teamId);
        var utc = DateTime.UtcNow;
        team.Status = TeamStatusValue.Confirmed;
        team.ConfirmedAtUtc = utc;
        team.ConfirmedByUserId = adminId;
        team.UpdatedAtUtc = utc;
        await dbContext.SaveChangesAsync();
        return teamId;
    }

    private async Task<Guid> GetReadyGameIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Games
            .Where(game => game.Status == GameStatusValue.Ready)
            .Select(game => game.Id)
            .FirstAsync();
    }

    private async Task SetReadyGameActiveAsync(Guid? activeTeamId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var game = await dbContext.Games.FirstAsync(game => game.Status == GameStatusValue.Ready);
        var utc = DateTime.UtcNow;
        game.Status = GameStatusValue.Active;
        game.StartedAtUtc = utc;
        game.ActiveTeamId = activeTeamId;
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> GetFirstSlotIdForReadyGameAsync()
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.GameTeamSlots
            .Where(slot => slot.GameId == gameId)
            .OrderBy(slot => slot.SlotIndex)
            .Select(slot => slot.Id)
            .FirstAsync();
    }

    private async Task<Guid> GetSlotIdByIndexAsync(int slotIndex)
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.GameTeamSlots
            .Where(slot => slot.GameId == gameId && slot.SlotIndex == slotIndex)
            .Select(slot => slot.Id)
            .FirstAsync();
    }

    private async Task<Guid> SeedPendingInvitationAsync(Guid invitedUserId)
    {
        await SeedReadyGameAsync();
        var gameId = await GetReadyGameIdAsync();
        var slotId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 3,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utc
            }
        );
        dbContext.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = invitationId,
                GameId = gameId,
                SlotId = slotId,
                InvitedUserId = invitedUserId,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = utc
            }
        );
        await dbContext.SaveChangesAsync();
        return invitationId;
    }

    private async Task<Guid> SeedPendingInvitationForReadyGameAsync(Guid invitedUserId, Guid slotId)
    {
        var gameId = await GetReadyGameIdAsync();
        var invitationId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = invitationId,
                GameId = gameId,
                SlotId = slotId,
                InvitedUserId = invitedUserId,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
        return invitationId;
    }

    private async Task<Guid> SeedPlayerInvitationAsync(Guid teamId, Guid ownerId, Guid invitedUserId)
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(item => item.Id == teamId);
        var invitationId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        dbContext.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = invitationId,
                GameId = gameId,
                SlotId = team.SlotId,
                TeamId = teamId,
                InvitedUserId = invitedUserId,
                InvitedByUserId = ownerId,
                InvitedByKind = InvitedByKindValue.Member,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = utc
            }
        );
        await dbContext.SaveChangesAsync();
        return invitationId;
    }
}
