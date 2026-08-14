import { Stack, Typography } from '@mui/material'

interface ParticipantNamesListProps {
  names: readonly string[]
  emptyLabel: string
  variant?: 'body2' | 'caption'
  dense?: boolean
  direction?: 'column' | 'row'
}

export function ParticipantNamesList({
  names,
  emptyLabel,
  variant = 'body2',
  dense = false,
  direction = 'column',
}: ParticipantNamesListProps) {
  if (names.length === 0) {
    return (
      <Typography variant={variant} color="text.secondary">
        {emptyLabel}
      </Typography>
    )
  }

  return (
    <Stack
      component="ul"
      direction={direction}
      spacing={direction === 'row' ? (dense ? 0.75 : 1) : dense ? 0 : 0.2}
      useFlexGap={direction === 'row'}
      sx={{
        m: 0,
        p: 0,
        listStyle: 'none',
        ...(direction === 'row' ? { flexWrap: 'wrap', justifyContent: 'center' } : {}),
      }}
    >
      {names.map((name, index) => (
        <Typography
          component="li"
          key={`${name}-${index}`}
          variant={variant}
          sx={dense ? { lineHeight: 1.25 } : undefined}
        >
          {name}
        </Typography>
      ))}
    </Stack>
  )
}
