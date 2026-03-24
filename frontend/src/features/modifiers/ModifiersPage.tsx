import { Box, Button, List, ListItem, ListItemText, Paper, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { PageStatePanel } from '../../shared/ui/PageStatePanel.tsx'
import { useModifiersPage } from './useModifiersPage.ts'

export function ModifiersPage() {
  const { t } = useTranslation()
  const { data, isError, isLoading, activateMutation } = useModifiersPage()

  if (isLoading) {
    return (
      <PageStatePanel
        title={t('nav.modifiers')}
        message={t('modifiers.loading')}
        showSpinner
      />
    )
  }

  if (isError || !data) {
    return (
      <PageStatePanel
        title={t('nav.modifiers')}
        message={t('modifiers.errorLoading')}
        tone="error"
      />
    )
  }

  return (
    <Paper sx={{ p: 3, display: 'flex', gap: 3, flexWrap: 'wrap' }}>
      <Box sx={{ flex: 1, minWidth: 260 }}>
        <Typography variant="h6" gutterBottom>
          {t('modifiers.availableTitle')}
        </Typography>
        <List dense>
          {data.available.map((mod) => (
            <ListItem
              key={mod.id}
              secondaryAction={
                <Button
                  size="small"
                  variant="contained"
                  disabled={activateMutation.isPending}
                  onClick={() => activateMutation.mutate(mod.id)}
                >
                  {t('modifiers.activateButton', { cost: mod.cost })}
                </Button>
              }
            >
              <ListItemText
                primary={t('modifiers.availableLabel', {
                  name: mod.name,
                  cost: mod.cost,
                })}
                secondary={mod.description}
              />
            </ListItem>
          ))}
        </List>
      </Box>

      <Box sx={{ flex: 1, minWidth: 260 }}>
        <Typography variant="h6" gutterBottom>
          {t('modifiers.activeTitle')}
        </Typography>
        {data.active.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('modifiers.emptyActive')}
          </Typography>
        ) : (
          <List dense>
            {data.active.map((a) => {
              const def = data.available.find((m) => m.id === a.modifierId)
              return (
                <ListItem key={a.id}>
                  <ListItemText
                    primary={def?.name ?? a.modifierId}
                    secondary={t('modifiers.activeFrom', {
                      user: a.triggeredBy,
                      time: new Date(a.activatedAt).toLocaleTimeString(),
                    })}
                  />
                </ListItem>
              )
            })}
          </List>
        )}
      </Box>
    </Paper>
  )
}

