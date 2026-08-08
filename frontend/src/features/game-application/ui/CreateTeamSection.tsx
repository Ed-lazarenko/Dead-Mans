import { Chip, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import {
  normalizeTeamNameInput,
  TEAM_NAME_MAX_LENGTH,
} from '../../game-registration/model/team-name.ts'

interface CreateTeamSectionProps {
  onCreate: (recruitmentOpen: boolean, name?: string) => void
  isCreating: boolean
}

export function CreateTeamSection({ onCreate, isCreating }: CreateTeamSectionProps) {
  const { t } = useTranslation()
  const [teamName, setTeamName] = useState('')
  const handleCreate = (recruitmentOpen: boolean) =>
    onCreate(recruitmentOpen, normalizeTeamNameInput(teamName))

  return (
    <SectionCard sx={{ height: '100%' }}>
      <Stack spacing={2.5} sx={{ height: '100%' }}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle1">{t('gameApplication.createTeamTitle')}</Typography>
            <Chip size="small" color="primary" label={t('gameApplication.createTeamChip')} />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {t('gameApplication.createTeamDescription')}
          </Typography>
        </Stack>

        <SectionCard inset variantStyle="dashed">
          <Stack spacing={1}>
            <TextField
              fullWidth
              size="small"
              label={t('gameApplication.teamNameField')}
              placeholder={t('gameApplication.teamNamePlaceholder')}
              value={teamName}
              slotProps={{ htmlInput: { maxLength: TEAM_NAME_MAX_LENGTH } }}
              onChange={(event) => setTeamName(event.target.value)}
            />
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.createTeamHelper')}
            </Typography>
          </Stack>
        </SectionCard>

        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} alignItems="stretch">
          <SectionCard
            inset
            sx={{
              flex: 1,
              display: 'flex',
              flexDirection: 'column',
              minHeight: 260,
              gap: 1.5,
              background:
                'linear-gradient(180deg, rgba(118,195,255,0.14) 0%, rgba(0,0,0,0.08) 100%)',
            }}
          >
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="subtitle2">{t('gameApplication.createOpenTeam')}</Typography>
              <Chip size="small" color="success" label={t('gameApplication.openTeamChip')} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.createOpenTeamDescription')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.openTeamHelper')}
            </Typography>
            <AppButton
              fullWidth
              disabled={isCreating}
              onClick={() => handleCreate(true)}
              sx={{ mt: 'auto' }}
            >
              {t('gameApplication.createOpenTeam')}
            </AppButton>
          </SectionCard>

          <SectionCard
            inset
            sx={{
              flex: 1,
              display: 'flex',
              flexDirection: 'column',
              minHeight: 260,
              gap: 1.5,
              background:
                'linear-gradient(180deg, rgba(255,214,102,0.12) 0%, rgba(0,0,0,0.08) 100%)',
            }}
          >
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="subtitle2">{t('gameApplication.createClosedTeam')}</Typography>
              <Chip size="small" label={t('gameApplication.closedTeamChip')} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.createClosedTeamDescription')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.closedTeamHelper')}
            </Typography>
            <AppButton
              fullWidth
              tone="secondary"
              disabled={isCreating}
              onClick={() => handleCreate(false)}
              sx={{ mt: 'auto' }}
            >
              {t('gameApplication.createClosedTeam')}
            </AppButton>
          </SectionCard>
        </Stack>
      </Stack>
    </SectionCard>
  )
}
