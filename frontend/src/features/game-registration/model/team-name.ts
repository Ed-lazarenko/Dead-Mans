export const TEAM_NAME_MAX_LENGTH = 48

export function normalizeTeamNameInput(value: string) {
  const normalized = value.trim().replace(/\s+/g, ' ')
  return normalized.length > 0 ? normalized : undefined
}

export function formatTeamNameWithFallback(teamName: string | null | undefined, fallback: string) {
  return normalizeTeamNameInput(teamName ?? '') ?? fallback
}
