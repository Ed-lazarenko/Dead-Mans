import { ThemeProvider } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { appTheme } from '../../../app/theme/appTheme.ts'
import i18n from '../../../i18n.ts'
import { GameFinishDialog } from './GameFinishDialog.tsx'

const mocks = vi.hoisted(() => ({
  fetchPreview: vi.fn(),
}))

vi.mock('../api/game-finish-api.ts', () => ({
  fetchGameFinishPreview: mocks.fetchPreview,
}))

const preview = {
  summary: {
    gameId: '00000000-0000-0000-0000-000000000001',
    gameTitle: 'Final game',
    gameStatus: 'active' as const,
    boardVersion: 7,
    finishedAtUtc: null,
    finishedByUserId: null,
    finishedByDisplayName: null,
    publicNote: null,
    calculationVersion: 1,
    completedRoundCount: 1,
    cancelledRoundCount: 0,
    totalKills: 2,
    totalBounties: 0,
    quizTotalPoints: 10,
    pendingQuizQuestionCount: 1,
    skippedQuizQuestionCount: 0,
    teams: [
      {
        teamId: '00000000-0000-0000-0000-000000000002',
        teamName: 'Alpha',
        teamSlotIndex: 1,
        participantNames: ['Player'],
        roundsPlayed: 1,
        bestScore: 100,
        penaltyTotal: 5,
        finalScore: 95,
        totalScore: 95,
        totalBonusDelta: 0,
        totalKills: 2,
        totalBounties: 0,
        placement: 1,
        lastFinishedAtUtc: '2026-09-06T00:00:00Z',
      },
      {
        teamId: '00000000-0000-0000-0000-000000000003',
        teamName: 'Bravo',
        teamSlotIndex: 2,
        participantNames: [],
        roundsPlayed: 0,
        bestScore: null,
        penaltyTotal: 0,
        finalScore: null,
        totalScore: 0,
        totalBonusDelta: 0,
        totalKills: 0,
        totalBounties: 0,
        placement: null,
        lastFinishedAtUtc: null,
      },
    ],
  },
  canFinish: true,
  blockers: [],
  warnings: [{ code: 'game_finish.unplayed_teams', count: 1 }],
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  mocks.fetchPreview.mockResolvedValue(preview)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameFinishDialog', () => {
  it('requires warning and irreversible confirmations and submits the server version', async () => {
    const onFinish = vi.fn().mockResolvedValue({})
    renderDialog(onFinish)

    expect(await screen.findByText('Bravo')).toBeVisible()
    expect(screen.getByText('Не играла')).toBeVisible()
    const submit = screen.getByRole('button', { name: 'Завершить игру навсегда' })
    expect(submit).toBeDisabled()

    fireEvent.click(screen.getByLabelText(/Не отмечены отыгравшими команд/))
    fireEvent.click(screen.getByLabelText(/Я понимаю, что в этой версии/))
    fireEvent.change(screen.getByLabelText(/Общий публичный комментарий/), {
      target: { value: 'Итоговый комментарий' },
    })
    expect(submit).toBeEnabled()
    fireEvent.click(submit)

    await waitFor(() => expect(onFinish).toHaveBeenCalledTimes(1))
    expect(onFinish).toHaveBeenCalledWith(
      expect.objectContaining({
        gameId: preview.summary.gameId,
        expectedBoardVersion: 7,
        acknowledgedWarningCodes: ['game_finish.unplayed_teams'],
        note: 'Итоговый комментарий',
      }),
    )
  })

  it('shows blockers and never enables completion', async () => {
    mocks.fetchPreview.mockResolvedValue({
      ...preview,
      canFinish: false,
      blockers: [{ code: 'game_finish.round_in_progress', count: 1 }],
      warnings: [],
    })
    renderDialog(vi.fn())

    expect(await screen.findByText(/Незавершённых раундов: 1/)).toBeVisible()
    fireEvent.click(screen.getByLabelText(/Я понимаю, что в этой версии/))
    expect(screen.getByRole('button', { name: 'Завершить игру навсегда' })).toBeDisabled()
  })
})

function renderDialog(onFinish: ReturnType<typeof vi.fn>) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <I18nextProvider i18n={i18n}>
      <ThemeProvider theme={appTheme}>
        <QueryClientProvider client={queryClient}>
          <GameFinishDialog
            open
            gameId={preview.summary.gameId}
            isFinishing={false}
            finishError={null}
            onClose={vi.fn()}
            onFinish={onFinish}
          />
        </QueryClientProvider>
      </ThemeProvider>
    </I18nextProvider>,
  )
}
