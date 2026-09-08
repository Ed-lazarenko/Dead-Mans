import type { DragEvent } from 'react'

export type RegistrationDragPayload =
  { kind: 'player'; userId: string } | { kind: 'team'; teamId: string }

const registrationDragMimeType = 'application/x-deadmans-registration'

export const defaultVisiblePlayersCount = 10
export const maxVisibleSearchResults = 18
export const minimumSearchLength = 2

export const teamActionButtonSx = {
  alignSelf: 'flex-start',
  flex: '0 0 auto',
  minHeight: { xs: 44, sm: 36 },
  whiteSpace: 'nowrap',
}

export const teamReorderButtonSx = {
  border: 1,
  borderColor: 'divider',
  color: 'text.secondary',
  minHeight: { xs: 44, sm: 36 },
  minWidth: { xs: 44, sm: 36 },
  backgroundColor: 'action.hover',
  transition: 'background-color 120ms ease, border-color 120ms ease, color 120ms ease',
  '&:hover': {
    borderColor: 'primary.main',
    color: 'primary.main',
    backgroundColor: 'action.selected',
  },
  '&.Mui-disabled': {
    borderColor: 'divider',
    backgroundColor: 'transparent',
    opacity: 0.38,
  },
}

export const createTeamButtonSx = {
  alignSelf: 'flex-start',
  flex: '0 0 auto',
  minHeight: { xs: 44, sm: 36 },
  whiteSpace: 'nowrap',
  width: { xs: '100%', sm: 'auto' },
}

export function writeRegistrationDragPayload(
  event: DragEvent<HTMLElement>,
  payload: RegistrationDragPayload,
) {
  event.dataTransfer.effectAllowed = 'move'
  event.dataTransfer.setData(registrationDragMimeType, JSON.stringify(payload))
  event.dataTransfer.setData(
    'text/plain',
    payload.kind === 'player' ? payload.userId : payload.teamId,
  )
}

export function readRegistrationDragPayload(
  event: DragEvent<HTMLElement>,
): RegistrationDragPayload | null {
  const rawPayload = event.dataTransfer.getData(registrationDragMimeType)
  if (!rawPayload) {
    return null
  }

  try {
    const parsed = JSON.parse(rawPayload) as Partial<RegistrationDragPayload>
    if (parsed.kind === 'player' && typeof parsed.userId === 'string') {
      return { kind: 'player', userId: parsed.userId }
    }

    if (parsed.kind === 'team' && typeof parsed.teamId === 'string') {
      return { kind: 'team', teamId: parsed.teamId }
    }
  } catch {
    return null
  }

  return null
}
