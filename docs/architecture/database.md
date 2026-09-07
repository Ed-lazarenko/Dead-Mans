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
  - `game_modifier_activations` stores immutable, round-scoped modifier purchases,
    including owner/initiator, frozen definition revision, full BehaviorV2/catalog
    snapshot and cancellation/refund audit.
  - `game_enabled_modifiers` and `game_enabled_questions` store per-game enabled catalog rows;
    modifier rows also preserve the actor, timestamp and reason for a game-scoped emergency disable.
  - `game_team_slots`, `game_teams`, `game_team_members` and
    `game_team_invitations` store registration and queue state.

## Core Aggregates

- Auth and access: `users`, `roles`, `user_roles`.
- Game lifecycle: `games`, `game_boards`, `game_board_cells`,
  `game_board_cell_media`, `game_finalizations`, `game_team_final_results`.
- Teams and registration: `game_team_slots`, `game_teams`, `game_team_members`,
  `game_team_invitations`.
- Round history and leaderboard facts: `game_rounds`,
  `game_round_participants`, `game_round_cell_media`,
  `game_round_modifier_results`.
- Modifier catalog and runtime: `modifier_definitions`, append-only
  `modifier_definition_versions`, immutable conflict-name snapshots,
  `modifier_definition_version_conflicts`, `game_enabled_modifiers`,
  `game_modifier_activations`.
- Quiz catalog and runtime: `question_categories`, `question_definitions`,
  `game_enabled_questions`, `game_quiz_rounds`,
  `game_quiz_manual_awards`.
- Media catalog: `media_assets`.

## Integrity Rules

- Historical facts are preserved. Round rows keep denormalized snapshots for card,
  team, participant, modifier and media details that leaderboards/history need.
  `game_rounds.empty_card_penalty_applied` stores the resolved fact that a
  completed card had no positive base or modifier score and therefore used its
  card value as a penalty; the penalty amount is derived from the round
  `base_score`.
- New game completions preserve one authoritative `game_finalizations` record and one
  `game_team_final_results` row per confirmed team. The unique request ID provides
  idempotency; display names, team names, slots and rosters are copied into the snapshot.
  Legacy finished games without these rows remain readable through round-derived history.
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
  - nonterminal round lifecycle is `awaiting_modifiers` → `preparing` →
    `in_progress` → `reviewing_results`; every mutation advances a monotonic
    `version` and lifecycle timestamps are checked against status;
  - partial unique index `ux_game_rounds_single_nonterminal_game` permits at most
    one nonterminal round per game, including races outside the application lock;
  - completed/cancelled `game_rounds` require final resolution data;
  - an empty-card penalty can only be marked on completed rounds;
  - pending modifier results cannot have resolver data, terminal modifier results
    must have it;
  - quiz round answer fields must match the quiz round status.
- Modifier purchase rows are never deleted to perform a refund:
  - status is `active`, `consumed` or `cancelled`;
  - `round_id` and owner/initiator IDs are mandatory;
  - a cancelled purchase keeps cancellation actor/time/reason and a refund in the
    inclusive range `0..activation_cost_snapshot`;
  - the current full-refund commands require `refund_amount = activation_cost_snapshot`,
    and timestamp/refund ordering is protected by database checks.
- Modifier content is revisioned and snapshot-based:
  - `modifier_definitions.current_version_id` selects the current revision; unique
    `(modifier_id, revision)`, composite ownership FKs, positive revisions, and database
    immutability triggers protect historical rows;
  - the stable root contains identity, archive state and audit only; version content is never
    duplicated into mutable root columns or a mutable conflict projection;
  - `ready -> active` pins the whole enabled set, and activation/runtime calculations resolve
    price, limit, compatibility, formula and display fields only from that game binding;
  - `modifier_definition_versions.behavior_v2_json` is strict schema version `2`; formulas are
    pinned by code/version with typed parameters, and normalized tags are stored separately;
  - activation rows freeze the revision, BehaviorV2, name, description, category, command,
    icon and tags at purchase time;
  - result rows copy only from the activation snapshot and require both a definition revision
    and BehaviorV2 snapshot. Missing or invalid snapshots fail closed instead of being silently
    recalculated from the current catalog.
- A modifier definition included in the active game is content-locked until that game
  finishes or is archived. Emergency disable is deliberately stored on
  `game_enabled_modifiers`, blocks only new activations for that game and cannot rewrite
  the first actor/time/reason audit; existing activations and snapshots remain unchanged.

## Concurrency Policy

- PostgreSQL remains the concurrency boundary for registration and lifecycle
  transitions.
- Team join/move operations lock affected team/slot rows with `SELECT ... FOR UPDATE`
  before validating capacity or occupancy.
- Game lifecycle transitions lock the target `games` row before validating and
  changing state. Every active-game mutation uses the same order: `game` first, then
  `round` / `quiz` / `cell`. After waiting for the game lock it rechecks that the game
  is still active.
- Round transitions, modifier activation and modifier cancellation subsequently share a
  `SELECT ... FOR UPDATE` lock on the target `game_rounds` row. Commands carrying
  `expectedRoundVersion` reject stale writers with `409`; already-applied refunds
  are recognized before that rejection and remain idempotent.
- Catalog create/update/archive and game start share one transaction-scoped PostgreSQL advisory
  lock; start also locks its game row. Compatibility cascades are all-or-nothing and recheck
  every affected definition after the lock. Modifier activation locks the active game and ordering round,
  then rejects an emergency-disabled enabled row before charging points.
- Modifier-history reads are separate `AsNoTracking` projections. Revision and archive keyset
  indexes plus case-insensitive trigram GIN indexes keep pagination and bounded search predictable as
  the catalog grows; command-count and `EXPLAIN` regression tests guard against N+1 and plan drift.
- `game_round_transition_audits` is append-only lifecycle evidence keyed by
  `(round_id, sequence)`; unique `(round_id, resulting_round_version)` prevents two
  transitions from claiming the same version.
- Technical cancellation is one transaction: it terminally cancels the round, fully
  refunds every non-cancelled activation, retires the board cell as `cancelled`, clears
  the active team and advances both round and board versions. Structured reason fields
  and database checks keep cancellation records internally consistent.
- Whole-game finalization is also one transaction: pending quiz questions are skipped,
  the immutable result is inserted, the active team is cleared, the game becomes
  `finished`, and the board version advances. A failed snapshot insert rolls back every
  one of those writes.
- Partial unique indexes still act as the final guard for singleton draft/ready/active
  games, one active team per slot, one active team membership per user, and one
  pending invitation per user/game.

## PostgreSQL Tests

- Fast endpoint contract tests can keep using InMemory where database behavior is
  not under test.
- Persistence-boundary tests use a real temporary PostgreSQL database
  (`deadmans_tests_*`) created from the current EF migrations.
- The Postgres suite verifies representative FK/check failures and a concurrent
  last-slot team join scenario. It also races modifier activation against prepare and
  prepare against rebuild, and game completion against active-team selection. These
  prove that row locks plus versions prevent late purchases, post-finish mutations,
  lost transitions and misordered audit rows. A forced snapshot-insert failure verifies
  transactional rollback.

## Migration Policy

- The schema is evolved through an ordered additive EF migration chain; migrations
  that backfill historical facts fail closed when an old row cannot be mapped
  unambiguously.
- The BehaviorV2 migration blocks rollout for active unmapped custom definitions,
  legacy custom expressions and conflicting activation limits; no active row is silently
  archived or assigned an invented scoring formula. The local clean cutover subsequently
  removed the compatibility reader, so the runtime now accepts only complete V2 snapshots.
- The EF migrations history table is `__ef_migrations_history`.
- When changing the physical schema, update this document and the retention policy
  in the same change.
- Operational inventory, clone rehearsal, cutover and rollback are defined in
  `docs/runbooks/modifier-system-v2-rollout.md`. The local-only clean cutover applied
  `20260820184215_RemoveLegacyModifierCompatibility`; any future shared/production deployment
  must still satisfy the runbook's observation and backup gates before destructive cleanup.
