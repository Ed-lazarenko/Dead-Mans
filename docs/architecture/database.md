# Database Architecture

Dead-Mans uses PostgreSQL as the source of truth. Local development can rebuild the
database from a clean EF Core baseline migration; object storage is intentionally
outside of database resets and keeps card images/media.

## Naming

- Physical database names use `snake_case` for tables, columns, indexes, foreign
  keys and check constraints.
- EF entity names can stay domain-oriented (`GameRound`, `GameModifierActivation`),
  but storage names describe the product concept:
  - `game_rounds` stores played card rounds and their score snapshots.
  - `game_round_cell_media` stores immutable media URLs captured for a played round.
  - `game_modifier_activations` stores modifier activations during a game.
  - `game_enabled_modifiers` and `game_enabled_questions` store per-game enabled catalog rows.
  - `game_team_slots`, `game_teams`, `game_team_members` and
    `game_team_invitations` store registration and queue state.

## Core Aggregates

- Auth and access: `users`, `roles`, `user_roles`.
- Game lifecycle: `games`, `game_boards`, `game_board_cells`,
  `game_board_cell_media`.
- Teams and registration: `game_team_slots`, `game_teams`, `game_team_members`,
  `game_team_invitations`.
- Round history and leaderboard facts: `game_rounds`,
  `game_round_participants`, `game_round_cell_media`,
  `game_round_modifier_results`.
- Modifier catalog and runtime: `modifier_definitions`, `modifier_conflicts`,
  `game_enabled_modifiers`, `game_modifier_activations`.
- Quiz catalog and runtime: `question_categories`, `question_definitions`,
  `game_enabled_questions`, `game_quiz_rounds`,
  `game_quiz_manual_awards`.
- Media catalog: `media_assets`.

## Integrity Rules

- Historical facts are preserved. Round rows keep denormalized snapshots for card,
  team, participant, modifier and media details that leaderboards/history need.
- Catalog deletes are soft/archive operations (`is_deleted`, `deleted_at_utc`,
  `is_archived`) so old game history remains readable.
- Foreign keys from historical facts to global catalogs use restrictive delete
  behavior unless the child is a draft/runtime-only value.
- Check constraints validate state machines, non-negative scores/counts, soft-delete
  timestamp semantics and JSON-backed board dimensions where PostgreSQL can enforce
  the rule safely.
- Partial unique indexes enforce singleton active lifecycle states and prevent the
  same active user from occupying conflicting team membership/invitation states.
- `games.active_team_id` is protected by a composite FK to
  `game_teams(game_id, id)`, so a game cannot point at a team from another game.
- Registration rows enforce lifecycle facts:
  - `game_teams` status must match its confirmation/rejection/disband timestamps.
  - `game_team_invitations.pending` cannot have `responded_at_utc`; all terminal
    invitation statuses must have it.
  - `game_team_members.left_at_utc` cannot be earlier than `joined_at_utc`.
- Round and quiz history enforce resolution facts:
  - completed/cancelled `game_rounds` require final resolution data;
  - pending modifier results cannot have resolver data, terminal modifier results
    must have it;
  - quiz round answer fields must match the quiz round status.

## Concurrency Policy

- PostgreSQL remains the concurrency boundary for registration and lifecycle
  transitions.
- Team join/move operations lock affected team/slot rows with `SELECT ... FOR UPDATE`
  before validating capacity or occupancy.
- Game lifecycle transitions lock the target `games` row before validating and
  changing state.
- Partial unique indexes still act as the final guard for singleton draft/ready/active
  games, one active team per slot, one active team membership per user, and one
  pending invitation per user/game.

## PostgreSQL Tests

- Fast endpoint contract tests can keep using InMemory where database behavior is
  not under test.
- Persistence-boundary tests use a real temporary PostgreSQL database
  (`deadmans_tests_*`) created from the current EF migrations.
- The Postgres suite verifies representative FK/check failures and a concurrent
  last-slot team join scenario, proving that row locks prevent overfilling a team.

## Migration Policy

- The local baseline is a single `InitialCreate` migration.
- The EF migrations history table is `__ef_migrations_history`.
- When changing the physical schema, update this document and the retention policy
  in the same change.
