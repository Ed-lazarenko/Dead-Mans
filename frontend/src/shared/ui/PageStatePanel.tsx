import { CircularProgress, Paper, Stack, Typography } from '@mui/material'

interface PageStatePanelProps {
  title?: string
  message: string
  tone?: 'default' | 'error'
  showSpinner?: boolean
}

export function PageStatePanel({
  title,
  message,
  tone = 'default',
  showSpinner = false,
}: PageStatePanelProps) {
  return (
    <Paper sx={{ p: 3 }}>
      <Stack spacing={title ? 1 : 0} alignItems={showSpinner ? 'center' : 'stretch'}>
        {showSpinner ? <CircularProgress size={28} /> : null}
        {title ? <Typography variant="h6">{title}</Typography> : null}
        <Typography
          variant="body2"
          color={tone === 'error' ? 'error' : 'text.secondary'}
          textAlign={showSpinner ? 'center' : 'left'}
        >
          {message}
        </Typography>
      </Stack>
    </Paper>
  )
}
