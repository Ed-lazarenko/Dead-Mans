import { Stack, Typography } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import {
  gameRoundPostRoundActions,
  type GameRoundSummaryFormValues,
} from '../model/game-round-summary-form.ts'

export function GameRoundPostRoundSection({
  control,
}: {
  control: ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']
}) {
  const { t } = useTranslation()

  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.roundSummaryPostRoundTitle')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.roundSummaryPostRoundDescription')}
        </Typography>
        <Controller
          control={control}
          name="postRoundAction"
          render={({ field }) => (
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
              {gameRoundPostRoundActions.map((action) => (
                <AppButton
                  key={action}
                  type="button"
                  tone={field.value === action ? 'primary' : 'secondary'}
                  fullWidth
                  onClick={() => field.onChange(action)}
                  sx={{ minHeight: 58, justifyContent: 'flex-start', px: 1.5 }}
                >
                  <Stack alignItems="flex-start" spacing={0.35}>
                    <Typography variant="subtitle2" fontWeight={800}>
                      {t(`gameBoard.roundSummaryPostRoundOption.${action}.title`)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" textAlign="left">
                      {t(`gameBoard.roundSummaryPostRoundOption.${action}.description`)}
                    </Typography>
                  </Stack>
                </AppButton>
              ))}
            </Stack>
          )}
        />
      </Stack>
    </SectionCard>
  )
}
