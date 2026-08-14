import type { RegistrationPlayer } from '../../../shared/api/contracts/index.ts'

interface RegistrationPlayerSearchOptions {
  query: string
  minQueryLength: number
  limit: number
  includeAllWhenQueryEmpty?: boolean
  rankStartsWith?: boolean
  locale?: string
}

function sortRegistrationPlayers(players: readonly RegistrationPlayer[], locale?: string) {
  return [...players].sort((left, right) => compareRegistrationPlayers(left, right, locale))
}

export function searchRegistrationPlayers(
  players: readonly RegistrationPlayer[],
  {
    query,
    minQueryLength,
    limit,
    includeAllWhenQueryEmpty = false,
    rankStartsWith = false,
    locale,
  }: RegistrationPlayerSearchOptions,
) {
  const normalizedQuery = normalizeRegistrationPlayerQuery(query, locale)
  const isTooShort = normalizedQuery.length > 0 && normalizedQuery.length < minQueryLength

  if (isTooShort) {
    return {
      normalizedQuery,
      isTooShort,
      matches: [],
      visible: [],
      hiddenCount: 0,
    }
  }

  const sortedPlayers = sortRegistrationPlayers(players, locale)
  const matches =
    normalizedQuery.length === 0
      ? includeAllWhenQueryEmpty
        ? sortedPlayers
        : []
      : matchRegistrationPlayers(sortedPlayers, normalizedQuery, rankStartsWith, locale)
  const visible = matches.slice(0, limit)

  return {
    normalizedQuery,
    isTooShort,
    matches,
    visible,
    hiddenCount: Math.max(0, matches.length - visible.length),
  }
}

function normalizeRegistrationPlayerQuery(query: string, locale?: string) {
  return query.trim().toLocaleLowerCase(locale)
}

function compareRegistrationPlayers(
  left: RegistrationPlayer,
  right: RegistrationPlayer,
  locale?: string,
) {
  const displayNameOrder = left.displayName.localeCompare(right.displayName, locale)
  if (displayNameOrder !== 0) {
    return displayNameOrder
  }

  return left.login.localeCompare(right.login, locale)
}

function matchRegistrationPlayers(
  players: readonly RegistrationPlayer[],
  normalizedQuery: string,
  rankStartsWith: boolean,
  locale?: string,
) {
  return players
    .map((player) => {
      const displayName = player.displayName.toLocaleLowerCase(locale)
      const login = player.login.toLocaleLowerCase(locale)
      const includesDisplayName = displayName.includes(normalizedQuery)
      const includesLogin = login.includes(normalizedQuery)

      if (!includesDisplayName && !includesLogin) {
        return null
      }

      const startsWithDisplayName = displayName.startsWith(normalizedQuery)
      const startsWithLogin = login.startsWith(normalizedQuery)
      const rank = rankStartsWith && (startsWithDisplayName || startsWithLogin) ? 0 : 1
      return { player, rank }
    })
    .filter((entry): entry is { player: RegistrationPlayer; rank: number } => entry !== null)
    .sort((left, right) => {
      if (left.rank !== right.rank) {
        return left.rank - right.rank
      }

      return compareRegistrationPlayers(left.player, right.player)
    })
    .map((entry) => entry.player)
}
