import { Button, Stack, TextField, Typography } from '@mui/material'
import Autocomplete, { createFilterOptions } from '@mui/material/Autocomplete'
import type { FormEvent } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'

type ManualQuizAwardPlayer = components['schemas']['ManualQuizAwardPlayerDto']

interface ManualQuizAwardControlProps {
  isActiveGame: boolean
  players: readonly ManualQuizAwardPlayer[]
  isLoading: boolean
  isError: boolean
  isAwarding: boolean
  onAward: (input: { awardedToUserId: string; points: number }) => void
  showHeader?: boolean
}

const filterManualAwardPlayers = createFilterOptions<ManualQuizAwardPlayer>({
  limit: 30,
  stringify: (player) => `${player.displayName} ${player.login}`,
})

export function ManualQuizAwardControl({
  isActiveGame,
  players,
  isLoading,
  isError,
  isAwarding,
  onAward,
  showHeader = true,
}: ManualQuizAwardControlProps) {
  const { t } = useTranslation()
  const [selectedUserId, setSelectedUserId] = useState('')
  const [points, setPoints] = useState('')

  const selectedPlayer = players.find((player) => player.userId === selectedUserId) ?? null
  const selectedValidUserId = selectedPlayer?.userId ?? ''
  const pointsNumber = Number(points)
  const canAward =
    isActiveGame &&
    selectedValidUserId !== '' &&
    Number.isInteger(pointsNumber) &&
    pointsNumber > 0 &&
    !isAwarding

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!canAward) {
      return
    }

    onAward({
      awardedToUserId: selectedValidUserId,
      points: pointsNumber,
    })
    setPoints('')
  }

  return (
    <Stack component="form" spacing={1.25} onSubmit={handleSubmit}>
      {showHeader ? (
        <>
          <Typography variant="subtitle2">{t('gameBoard.manualQuizAwardTitle')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.manualQuizAwardDescription')}
          </Typography>
        </>
      ) : null}

      {!isActiveGame ? (
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.manualQuizAwardInactive')}
        </Typography>
      ) : isLoading ? (
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.manualQuizAwardLoading')}
        </Typography>
      ) : isError ? (
        <Typography variant="body2" color="error">
          {t('gameBoard.manualQuizAwardError')}
        </Typography>
      ) : players.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.manualQuizAwardNoPlayers')}
        </Typography>
      ) : (
        <>
          <Autocomplete
            size="small"
            fullWidth
            autoHighlight
            selectOnFocus
            options={players}
            value={selectedPlayer}
            filterOptions={filterManualAwardPlayers}
            disabled={isAwarding}
            noOptionsText={t('gameBoard.manualQuizAwardNoPlayerMatches')}
            getOptionLabel={(player) =>
              player.login ? `${player.displayName} · ${player.login}` : player.displayName
            }
            isOptionEqualToValue={(option, value) => option.userId === value.userId}
            onChange={(_, player) => setSelectedUserId(player?.userId ?? '')}
            slotProps={{
              popper: {
                sx: (theme) => ({
                  zIndex: theme.zIndex.modal + 1,
                }),
              },
            }}
            renderInput={(params) => <TextField {...params} label={t('common.entities.player')} />}
          />

          <TextField
            size="small"
            type="number"
            label={t('gameBoard.manualQuizAwardPointsLabel')}
            value={points}
            disabled={isAwarding}
            inputProps={{ min: 1, step: 1 }}
            onChange={(event) => setPoints(event.target.value)}
          />

          <Button
            type="submit"
            variant="contained"
            disabled={!canAward}
            sx={{ alignSelf: 'flex-start', minHeight: 36 }}
          >
            {isAwarding
              ? t('gameBoard.manualQuizAwardSaving')
              : t('gameBoard.manualQuizAwardAction')}
          </Button>
        </>
      )}
    </Stack>
  )
}
