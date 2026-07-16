import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import type { GameCardRunDetails } from '../../../shared/api/contracts/index.ts'
import { FormSelect, FormTextField, SectionCard } from '../../../shared/ui/index.ts'

interface SelectOption {
  value: string
  label: string
}

interface ActiveCardRunPanelProps {
  openCellOptions: readonly SelectOption[]
  eligibleTeamOptions: readonly SelectOption[]
  activeRun: GameCardRunDetails | null
  canManageCardRuns: boolean
  isLoadingTeams: boolean
  isStarting: boolean
  isFinalizing: boolean
  selectedCellId: string
  selectedTeamId: string
  finalStatus: 'completed' | 'cancelled'
  finalScoreInput: string
  notes: string
  onSelectedCellChange: (value: string) => void
  onSelectedTeamChange: (value: string) => void
  onFinalStatusChange: (value: 'completed' | 'cancelled') => void
  onFinalScoreInputChange: (value: string) => void
  onNotesChange: (value: string) => void
  onStartRun: () => void
  onFinalizeRun: (cardRunId: string) => void
  labels: {
    title: string
    idleDescription: string
    activeDescription: string
    openCell: string
    team: string
    start: string
    status: string
    finalScore: string
    notes: string
    complete: string
    noOpenCells: string
    noTeams: string
  }
}

export function ActiveCardRunPanel({
  openCellOptions,
  eligibleTeamOptions,
  activeRun,
  canManageCardRuns,
  isLoadingTeams,
  isStarting,
  isFinalizing,
  selectedCellId,
  selectedTeamId,
  finalStatus,
  finalScoreInput,
  notes,
  onSelectedCellChange,
  onSelectedTeamChange,
  onFinalStatusChange,
  onFinalScoreInputChange,
  onNotesChange,
  onStartRun,
  onFinalizeRun,
  labels,
}: ActiveCardRunPanelProps) {
  if (!canManageCardRuns) {
    return null
  }

  return (
    <SectionCard inset sx={{ mt: 2 }}>
      <Stack spacing={2}>
        <Box>
          <Typography variant="subtitle2" fontWeight={700}>
            {labels.title}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {activeRun ? labels.activeDescription : labels.idleDescription}
          </Typography>
        </Box>

        {activeRun ? (
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip label={`#${activeRun.teamSlotIndex}`} size="small" color="warning" />
              <Chip label={activeRun.status} size="small" variant="outlined" />
              <Chip label={`Base ${activeRun.baseScore}`} size="small" variant="outlined" />
            </Stack>

            <Typography variant="body2" color="text.secondary">
              {activeRun.participants.map((participant) => participant.displayName).join(', ')}
            </Typography>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <FormSelect
                label={labels.status}
                value={finalStatus}
                options={[
                  { value: 'completed', label: 'completed' },
                  { value: 'cancelled', label: 'cancelled' },
                ]}
                onChange={(value) => onFinalStatusChange(value as 'completed' | 'cancelled')}
              />
              <FormTextField
                label={labels.finalScore}
                value={finalScoreInput}
                onChange={(event) => onFinalScoreInputChange(event.target.value)}
                type="number"
              />
            </Stack>

            <FormTextField
              label={labels.notes}
              value={notes}
              onChange={(event) => onNotesChange(event.target.value)}
              multiline
              minRows={2}
            />

            <Button
              variant="contained"
              color="warning"
              disabled={isFinalizing}
              onClick={() => onFinalizeRun(activeRun.cardRunId)}
            >
              {labels.complete}
            </Button>
          </Stack>
        ) : (
          <Stack spacing={2}>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <FormSelect
                label={labels.openCell}
                value={selectedCellId}
                options={openCellOptions}
                onChange={onSelectedCellChange}
                disabled={openCellOptions.length === 0 || isStarting}
              />
              <FormSelect
                label={labels.team}
                value={selectedTeamId}
                options={eligibleTeamOptions}
                onChange={onSelectedTeamChange}
                disabled={isLoadingTeams || eligibleTeamOptions.length === 0 || isStarting}
              />
            </Stack>

            {openCellOptions.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {labels.noOpenCells}
              </Typography>
            ) : null}

            {eligibleTeamOptions.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {labels.noTeams}
              </Typography>
            ) : null}

            <Button
              variant="contained"
              disabled={
                isStarting ||
                selectedCellId === '' ||
                selectedTeamId === '' ||
                openCellOptions.length === 0 ||
                eligibleTeamOptions.length === 0
              }
              onClick={onStartRun}
            >
              {labels.start}
            </Button>
          </Stack>
        )}
      </Stack>
    </SectionCard>
  )
}
