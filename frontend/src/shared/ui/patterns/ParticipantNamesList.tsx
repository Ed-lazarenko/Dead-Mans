import { Stack, Typography } from '@mui/material'

interface ParticipantNamesListProps {
  names: readonly string[]
  emptyLabel: string
  variant?: 'body2' | 'caption'
}

export function ParticipantNamesList({
  names,
  emptyLabel,
  variant = 'body2',
}: ParticipantNamesListProps) {
  if (names.length === 0) {
    return (
      <Typography variant={variant} color="text.secondary">
        {emptyLabel}
      </Typography>
    )
  }

  return (
    <Stack component="ul" spacing={0.2} sx={{ m: 0, p: 0, listStyle: 'none' }}>
      {names.map((name, index) => (
        <Typography component="li" key={`${name}-${index}`} variant={variant}>
          {name}
        </Typography>
      ))}
    </Stack>
  )
}
