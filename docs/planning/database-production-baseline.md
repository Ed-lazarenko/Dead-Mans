# Database production baseline

Status: complete (2026-09-08)

This document is the source of truth for the pre-production database review. The
database is still local and disposable. The development migration chain has therefore
been replaced with one reviewed production baseline before the first public deployment.

## Scope and fixed product assumptions

- PostgreSQL remains the authoritative persistence layer.
- The current product remains a single-arena application: at most one non-deleted
  `draft` game and, separately, at most one user-visible current game across `ready`
  and `active`. Thus `active + draft` and `ready + draft` are valid, while
  `active + ready`, two drafts or two current games are impossible. A draft cannot
  move to `ready` before the current game reaches `finished`; archival never bypasses
  the lifecycle.
  Multi-tenancy is not introduced without an explicit product requirement and
  tenant/ownership model.
- Historical game facts are append-only or snapshot-based and must not change when
  a catalog item, user profile or media delivery URL changes.
- `users` is the canonical Twitch principal table, not proof of an authenticated app
  account. The bot may create a row on first chat activity with no login timestamp or
  roles; OAuth later reuses the same `twitch_user_id` row. A valid session and role,
  not mere row existence, are required for actions such as modifier activation.
- A quiz question remains open until its timer expires or the first correct answer
  arrives. Incorrect chat messages are transient and are not persisted.
- Quiz points are scoped to exactly one game and reset implicitly for every new game:
  the balance identity is `(game_id, user_id)` and points never carry over between
  games. The immutable ledger is the source of truth; cached balances, if introduced
  later, are projections only.
- Database constraints are the final integrity boundary. Application validation is
  useful for errors and UX, but cannot be the only guard against cross-aggregate data.

## Findings found and resolved

### P0/P1 integrity gaps

1. `game_rounds.team_id` does not prove that the team belongs to the same game.
2. `game_rounds.board_cell_id` does not prove that the cell belongs to the same game.
3. `game_modifier_activations.round_id` does not prove that the round belongs to
   `game_modifier_activations.game_id`.
4. `game_round_modifier_results` does not prove that its round, activation and
   modifier describe the same activation instance.
5. Quiz history used to mix the asked-question fact and its mutable resolution.
   These are now separated into an immutable round and an optional first-correct-answer
   fact; incorrect, late and ignored Twitch messages intentionally remain transient.
6. A quiz round reads the live mutable `question_definitions` row. Editing a question
   can therefore change how an already asked historical question is displayed or
   interpreted.
7. Quiz points are split between accepted quiz rows, manual awards and modifier
   activations. This makes a complete point history a multi-table reconstruction
   instead of one auditable ledger.
8. `game_round_cell_media` stores a delivery URL instead of immutable object identity.
   A host, CDN or signing-policy change can make historical media point at the wrong
   location or become unusable.

All eight gaps are closed in `20260908003848_ProductionBaseline`: aggregate ownership is
enforced by composite keys, quiz facts and points are normalized and immutable, historical
question/media values are frozen, and the unified ledger is the only quiz-balance source.

### P2 structural debt removed

- Derived counters were removed from `question_definitions`; quiz history owns those facts.
- `game_quiz_manual_awards` was replaced by typed entries in the unified immutable ledger.
- Notifications now have a typed/versioned payload plus game and correlation identity.
- Board cells, team slots and media assets have explicit domain checks and lifecycle data.
- Lifecycle timestamps and state combinations are constrained in PostgreSQL.
- Relational object names are explicit and stable; the final catalog has no truncated names.
- The 21 pre-release migrations and their compatibility/backfill paths were replaced by
  the single production baseline.

## Target aggregate map

### Identity and authorization

- `users`
- `roles`
- `user_roles`

Twitch account data stays on `users` for the current authentication boundary. This
avoids duplicating the provider subject ID and gives pre-login quiz points the same
stable owner that OAuth will later use.

### Game configuration and runtime

- `games`
- `game_boards`
- `game_board_cells`
- `game_board_cell_media`
- `game_team_slots`
- `game_teams`
- `game_team_members`
- `game_team_invitations`
- `game_enabled_modifiers`
- `game_enabled_questions`

Composite ownership keys must prevent a child from pointing into another game.

### Immutable played-game history

- `game_rounds`
- `game_round_participants`
- `game_round_cell_media`
- `game_modifier_activations`
- `game_round_modifier_results`
- `game_round_transition_audits`
- `game_finalizations`
- `game_team_final_results`

Round media snapshots store bucket, object key, MIME type, size and display ordering;
URLs are generated at read time.

### Quiz catalog and runtime

- `question_categories`
- `question_definitions`
- `question_accepted_answers`
- `game_enabled_questions`
- `game_quiz_rounds`
- `game_quiz_correct_answers`
- `game_quiz_point_ledger_entries`

`game_enabled_questions` freezes question text, category, answer set, reward and
question revision for a ready/active game. `game_quiz_rounds` freezes the delivered
question again as the fact that was actually asked. The first correct answer is an
append-only fact and keeps the provider/channel/message identity, the credited Twitch
principal, identity snapshots, normalized answer and receipt time. Incorrect attempts
are evaluated in memory and discarded. Only ledger entries change the point balance.

### Catalogs and delivery

- `modifier_definitions`
- `modifier_definition_versions`
- `modifier_definition_version_conflicts`
- `media_assets`
- `game_user_notifications`

Modifier revisions remain immutable. Notifications use a typed event name plus a
versioned JSON payload and correlation fields instead of adding nullable columns for
every new feature.

## Quiz runtime invariants

- At most one open quiz round exists per game.
- Ask order is unique and positive within a game.
- The same catalog question is asked at most once per game in the current rules, but
  each round can have at most one persisted correct answer.
- Delivery source is independent of the human/system actor that initiated it.
- A provider message ID is idempotent within provider and channel.
- A correct answer always refers to a Twitch principal in `users`. Spending additionally
  requires an authenticated session for that same principal.
- A terminal quiz round has a close timestamp. Answered rounds have exactly one correct
  answer; timeout/skipped rounds do not.
- Question evaluation always uses the frozen accepted-answer set, never live catalog
  data.
- Exactly one positive answer-reward ledger entry can reference a correct answer.
- Manual adjustments require an actor, reason and idempotency key.
- Modifier purchase/refund ledger entries are unique per activation and keep signed
  deltas. Free purchases do not need zero-value ledger rows.
- The running balance chain is isolated by `(game_id, user_id)`. Its first row starts
  from zero, so neither rewards nor unspent points can leak into another game.
- Ledger rows, correct answers and asked-question facts cannot be updated or deleted.

## Completed baseline cutover

1. Entity configurations and all application persistence paths were adapted.
2. PostgreSQL boundary, migration, immutability and concurrency coverage was added.
3. The 21 pre-release migrations were removed from the working tree; Git history remains
   the archive.
4. `20260908003848_ProductionBaseline` became the only migration and includes the reviewed
   PostgreSQL functions, triggers, partial indexes and extensions.
5. A clean `up -> down -> up` migration cycle passed; EF reports no pending model changes.
6. The full backend suite passed: 387 tests. The PostgreSQL subset passed: 30 tests.
7. The generated frontend transport is current; formatting, type checking, lint, i18n,
   283 frontend tests with coverage, dead-code analysis and production build all passed.
8. A fresh audit database contained 33 tables, 363 columns, 166 indexes, 291 constraints,
   56 user triggers and 37 `deadmans_*` functions. It contained only the three technical
   roles and no application data.
9. Catalog audit found zero unvalidated constraints, invalid indexes, tables without a
   primary key, unindexed foreign keys, duplicate/prefix-redundant indexes, truncated
   relational names, timestamp-without-time-zone columns, unbounded `varchar`, nullable
   arrays, functions without a fixed `search_path`, or security-definer functions.
10. `pg_dump`/`pg_restore` into another empty database succeeded. All 948 compared schema
    objects (tables, typed columns, constraints, indexes, triggers, functions and required
    extensions) matched exactly.

After the first public deployment, applied migrations are immutable and all future
schema changes are additive forward migrations.
