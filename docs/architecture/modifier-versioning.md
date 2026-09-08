# Immutable modifier revisions

This document is the canonical design for modifier catalog versioning, game pinning,
history, authorization, and retention.

## Aggregate and ownership

`modifier_definitions` is the stable aggregate root. Its `id` never changes and its
`current_version_id` points to the current immutable row. Archive state and definition
creation/archive audit live on the root. It deliberately has no name, price, behavior, tags,
compatibility, revision, or other mutable content columns: the current version is the only
catalog source of truth.

`modifier_definition_versions` stores every normalized content save. `(modifier_id,
revision)` is unique and revision is positive. Every row contains the complete name,
description, category, emoji, command, price, limit, tags, typed `BehaviorV2`, author id,
author-name snapshot, timestamp, optional 500-character note, semantic `changed_fields`,
change type, and optional cascade source. PostgreSQL triggers reject `UPDATE` and `DELETE`;
`ApplicationDbContext` rejects the same state transitions before SQL is generated.

`modifier_definition_version_conflicts` stores the conflicting stable id and its name
snapshot. Conflict rows are immutable with their parent version. Symmetry is maintained by
transactional version cascades; there is no second mutable compatibility projection.

Current catalog and revision history use dedicated `AsNoTracking` read projections. Command
paths use the shared modifier projector/resolver and never invent fallback values from the
current catalog for a played game.

## Writes and concurrency

Create, edit, archive, compatibility cascade, `draft -> ready`, and `ready -> active` take the
same PostgreSQL transaction-scoped advisory lock. Lifecycle transitions additionally lock the
game row. After the
locks are held, commands re-read revision, archive state, lifecycle state, active-game
bindings, and every referenced conflict target.

- A meaningful normalized edit inserts revision `N + 1`; a no-op returns the current
  revision without inserting or publishing.
- Update and archive require `expectedRevision`; stale writers receive
  `409 game_modifier_revision_stale`.
- Compatibility is symmetric. A change inserts the initiating `edited` revision and a
  `compatibility_cascade` revision for every affected definition in one transaction.
- If the initiating definition is used by an active game, the operation returns
  `game_modifier_content_locked`. If another cascade side is used, it returns
  `game_modifier_compatibility_locked`. Any error rolls back all sides.
- Archived definitions return `game_modifier_archived` for edits and are excluded from
  future catalog/setup selection. Restore and old-version mutation are intentionally absent.

Transactions end before DTO serialization and before SignalR publication. Cancellation is
propagated through controller, application, EF, and realtime calls.

## Game lifecycle and historical truth

`game_enabled_modifiers.modifier_version_id` and `version_pinned_at_utc` are null only in
`draft`. During `draft -> ready`, the complete enabled set is pinned to the latest committed
revisions in the same transaction as the publication transition. This is the freeze boundary
because registration makes the game user-visible. A ready or active game with a missing binding
is inconsistent; activation fails closed and `ready -> active` returns
`409 game_modifier_version_binding_missing`.

Activation resolves the version through the game's enabled row and copies the version id to
`game_modifier_activations.modifier_version_id`. Price charging, limits, compatibility,
formula, scoring, display data, and behavior are all read from the pinned revision. Existing
activation/result snapshot columns remain immutable audit evidence and make history independent
from future catalog changes.

Game history reports:

- `complete` when every enabled modifier has a pinned version, including modifiers that were
  never activated;
- `legacy_unavailable` only as a defensive response for externally imported/corrupt data that
  lacks the set. The empty production baseline and publication checks never create this state,
  and the reader never fills gaps from the current catalog.

Each complete snapshot includes full configuration, conflict-name snapshots, successful and
cancelled activation counts, result participation, and emergency-disable state. Later edits or
archive operations cannot change labels, cards, formulas, or totals of earlier games.

## Read API and query shape

Authenticated users can read:

- `GET /api/game/modifiers/history`
- `GET /api/game/modifiers/{modifierId}/versions`
- `GET /api/game/modifiers/{modifierId}/versions/{revision}`
- `GET /api/game/modifiers/{modifierId}/versions/{revision}/games`

Lists default to 20 and cap at 100. Search is capped at 100 characters and cursors at 512.
Pagination is keyset-based: definition and related-game pages use timestamp plus id; version
pages use descending revision. List projections omit descriptions and `BehaviorV2`; semantic
changed-field names are persisted with the immutable version. Full configuration and conflict
snapshots are loaded only by detail. Counts are correlated server aggregates, and all history
queries use `AsNoTracking` projections. Trigram GIN indexes back bounded substring search;
archive/timestamp/id and revision/id indexes back stable keyset traversal. Query counts for
catalog history, timeline, detail, conflicts, and related games remain constant as revision
volume grows, and PostgreSQL regression tests assert the revision index plan.

The existing `GET /api/game/modifiers/catalog` still returns only current, non-archived
definitions. Only `admin` may create, edit, or archive. History is available to every
authenticated role. All ids and `(modifierId, revision)` pairs are validated server-side;
unknown or mismatched values return `404 game_modifier_not_found`.

## Security and realtime

Write DTOs contain only editable content, `expectedRevision`, and `changeNote`; audit and
archive fields are server-owned. The author comes exclusively from the authenticated principal
and is revalidated against the active user record. Cookie-authenticated writes keep the
existing authentication and CSRF boundary. Error mapping exposes stable codes without SQL,
stack traces, or request bodies. The UI renders notes as React text, never injected HTML.

After a successful commit the game-board hub receives best-effort
`modifierCatalogChanged { modifiers, occurredAtUtc }`. Each item carries stable id, revision,
and archive state; cascades include every changed side. The bounded realtime guard logs a
publish failure without rolling back PostgreSQL. Clients invalidate catalog, setup, modifier
history, and detail queries and then refetch the source of truth.

## Migration and rollback

The first public database is created by `20260908003848_ProductionBaseline`; there is no
modifier data backfill and no compatibility reader. The baseline seeds no modifier definitions.
Definitions are created through the supported catalog workflow, every content save starts or
advances immutable revision history, and `draft -> ready` pins the selected set.

Before the first deployment the database may be dropped and rebuilt from this baseline. After
deployment, the baseline is immutable and every change uses a new forward migration. Rollback of
data-bearing revision history is performed by restoring a verified PostgreSQL backup, never by
folding immutable revisions into a mutable legacy shape.

## Required verification

The release gate is the backend suite (including real PostgreSQL migration, trigger, FK, index,
query-count, plan, and race tests), generated HTTP/realtime transports, frontend check, both
Windows launchers, and the v1/game-1 -> v2/game-2 smoke path. Do not deploy when any part of
that gate is red.
