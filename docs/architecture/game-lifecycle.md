# Game lifecycle and finalization

The persisted lifecycle is `draft → ready → active → finished`. `finished` is terminal:
this version deliberately has no reopen command.

## Publication freeze at ready

`draft → ready` is the publication boundary because a ready game is already visible to users.
That transaction takes the shared modifier-catalog advisory lock and a row lock on the game,
then freezes question snapshots and pins one current immutable revision for every enabled
modifier with the same publication timestamp. Later catalog edits or archive operations cannot
change the published game. `ready → active` validates that the complete enabled set is pinned
and fails closed on any missing binding. PostgreSQL additionally requires exactly one complete
closed board and at least one team slot at publication. Activation requires at least one confirmed
team, no forming teams, pending invitations or pending disband request, and every confirmed roster
inside the game's min/max limits. Published setup and the active/finished roster cannot be edited.
See [`modifier-versioning.md`](modifier-versioning.md) for runtime, compatibility and history rules.

## Finalization API

Only an authenticated `admin` can call:

- `GET /api/game/lifecycle/games/{gameId}/finish-preview`
- `POST /api/game/lifecycle/games/{gameId}/finish`

The command carries `expectedBoardVersion`, a unique `requestId`, acknowledged warning
codes and an optional public note of at most 2000 characters. The server never accepts
manual scores, kills, bounties, placements or modifier results during finalization.

Preview and commit use the same `GameTeamResultCalculator`. A team's official score is:

```text
best completed-round result before penalties
− penalties from all completed rounds
= final score
```

Cancelled rounds are excluded. A team without a completed round has a null score and
placement and is displayed as "Did not play". Equal final scores share a competition
placement (`1, 1, 3`); the existing best/total/latest/slot ordering is only a stable
display order inside a tie. Quiz points are reported separately and never enter team
placement.

An unfinished round or an unarchived modifier purchase attached to a terminal round is a
hard blocker. Unplayed teams and a game with zero completed rounds are warnings that must
be acknowledged using their stable codes. The commit recomputes all conditions after
acquiring the database lock, so a preview is advisory rather than a security boundary.

## Atomic commit and idempotency

Finalization locks the target `games` row, rechecks active status and board version, then
in one transaction:

1. validates round and modifier state and warning acknowledgements;
2. closes an `asked` quiz round as `skipped` if its timer is still live, otherwise as `timeout`;
3. writes one `game_finalizations` row plus team rows in `game_team_final_results`;
4. clears `games.active_team_id`;
5. sets `games.status = finished` and `finished_at_utc`;
6. increments the board version.

The stored result includes the finisher identity/display-name snapshot, note, algorithm
version, aggregate counts and immutable team/roster/result snapshots. `request_id` is
unique. Repeating a finish command for a game that already has a snapshot returns the
existing snapshot without replacing its note or numbers.

The production baseline is empty, so every persisted `finished` game must have this snapshot.
Deferred PostgreSQL checks reject a finish without it, incomplete team coverage, open runtime
state, timestamp disagreement or aggregate counts that differ from immutable round/quiz/ledger
facts. The history reader keeps a defensive fallback only for externally imported/corrupt data;
the supported lifecycle never creates such a row.

## UI and realtime

The admin game-management panel opens a server preview and requires each warning plus an
explicit irreversible-action confirmation. After success the board stays visible and
read-only and links to `/panel/game-history?gameId={gameId}`. All authenticated users can
read that history and the public note.

After the transaction commits, the server best-effort publishes
`gameLifecycleChanged { gameId, status, boardVersion, occurredAtUtc }`. Clients invalidate
the board, round, queue, registration, history, quiz-derived history, modifier and finish
preview caches. A realtime transport failure is logged and never rolls back the database
commit.
