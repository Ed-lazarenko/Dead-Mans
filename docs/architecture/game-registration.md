# Game registration

## Game lifecycle

`draft` → `ready` → `active` → `finished`

- **ready**: team registration (slots, teams, invitations). Board is visible; cells are not opened.
- **active**: gameplay (`POST /api/game/cells/{cellId}/open`).

Admin transitions (`POST`, admin role):

- `/api/game/lifecycle/open-registration` — draft → ready
- `/api/game/lifecycle/start` — ready → active
- `/api/game/lifecycle/finish` — active → finished

## Database

- `games`: `ReadyAtUtc`, `MinPlayersPerTeam`, `MaxPlayersPerTeam`
- `game_participation_slots`: public / reserved slots per game
- `game_teams`: `forming` | `confirmed` | `rejected` | `disbanded`; rejected/disbanded rows remain for history
- `game_team_members`: equal players (no captain role), with `JoinedAtUtc` / `LeftAtUtc` membership history
- `game_participation_invitations`: unified admin invite flow

Partial unique indexes: one `draft`, one `ready`, one `active` game at a time; one occupying team (`forming`/`confirmed`) per slot; one active membership per player/game.

## Registration API

- `GET /api/game/registration` — snapshot for the ready game
- `POST /api/game/registration/teams` — create team on a public slot
- `POST /api/game/registration/teams/{teamId}/join` — open room only
- `POST /api/game/registration/teams/leave` — while game is ready
- `GET /api/game/registration/teams` — legacy team list
- `GET /api/game/registration/admin` — slot-oriented admin snapshot with available players
- `POST /api/game/registration/admin/teams` — create an empty open or closed team
- `POST /api/game/registration/admin/teams/{teamId}/assign` — assign a free player or move a player between active teams
- `POST /api/game/registration/admin/teams/{teamId}/move` — move a team to another slot, swapping with the occupying team when needed
- `POST /api/game/registration/teams/{teamId}/confirm` / `reject` — approve or reject a team for play
- `POST /api/game/registration/invitations` — create admin invitations for reserved or curated flows

Draft setup creates six default public slots (`GameRegistrationDefaults`). Team size is enforced from the ready-game configuration, and the current baseline is 2 players per team.

## Panel routes

- `/panel/game-application` — player entry flow plus admin roster management when the current user has game setup capability
- `/panel/team-registrations` — dedicated admin registration workspace backed by the same slot snapshot and actions

## Current UI behavior

- Players choose between an open team and a closed team with clearer intent text.
- Open team means any eligible player can join until the configured team size is reached.
- Closed team means the roster is curated by invitation or by an admin assignment.
- Admins work in a slot-centric management panel with available players, team moves, slot swaps, empty-team creation, and approve/reject actions in one place.

## Known future work

- player-to-player invitations for closed teams;
- explicit registration settings in the game setup/global settings UI;
- clearer read-only history and audit presentation after registration closes.

## Layering and contracts

- **Transport**: `backend/openapi/deadmans.v1.yaml` documents `/api/game/registration` and `/api/game/lifecycle/*`. Regenerate frontend transport artifacts with `npm --prefix frontend run generate:transport`.
- **HTTP**: thin controllers (`GameRegistrationController`, `GameLifecycleController`); registration errors map via `Api/Mapping/GameRegistrationErrorMapping.cs` with stable `code` fields in `ErrorResponse`; DTOs via `Api/Mapping/GameRegistrationMapping.cs`.
- **Application**: `GameRegistrationService` / `GameLifecycleService` own registration rules and lifecycle preconditions; ports `IGameRegistrationService`, `IGameLifecycleService`.
- **Infrastructure**: `IGameRegistrationReadStore` + `IGameRegistrationPersistence`, `IGameLifecycleReadStore` + `IGameLifecyclePersistence`; slot seeding via `GameParticipationSlotInitializer` in `Infrastructure/Persistence/`.
- **History**: admin reject marks a team as `rejected`, closes active memberships, and cancels pending team invitations. Player leave marks `LeftAtUtc`; if the last active member leaves, the team becomes `disbanded`. Rows are preserved so future player/team/game history can be built from the same tables.
- **Frontend**: transport in `frontend/src/features/game-registration/api/`; UI in `game-application/` and `team-registrations/`. The admin panel is reused across both admin entry points. A missing ready-game snapshot (`404`) renders a normal unavailable state without disabled mock controls.
