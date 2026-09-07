import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { ModifierHistoryPage } from './ModifierHistoryPage.tsx'

const modifierId = '10000000-0000-0000-0000-000000000001'
const versionId = '20000000-0000-0000-0000-000000000001'
const gameId = '30000000-0000-0000-0000-000000000001'

beforeAll(async () => i18n.changeLanguage('ru'))

vi.mock('./api/modifier-history-api.ts', () => ({
  fetchModifierHistory: vi.fn(async () => ({
    items: [
      {
        modifierId,
        currentRevision: 2,
        name: 'Архивная редакция',
        category: 'round',
        iconEmoji: '🧭',
        activationCost: 9,
        isArchived: true,
        createdAtUtc: '2026-08-01T09:00:00Z',
        archivedAtUtc: '2026-09-02T09:00:00Z',
        versionCount: 2,
        gamesCount: 1,
        activationsCount: 1,
      },
    ],
    nextCursor: null,
  })),
  fetchModifierVersions: vi.fn(async () => ({
    items: [
      {
        versionId,
        modifierId,
        revision: 2,
        name: 'Архивная редакция',
        createdAtUtc: '2026-09-01T09:00:00Z',
        createdByUserId: null,
        createdByDisplayName: 'Администратор',
        changeNote: null,
        changeType: 'compatibility_cascade',
        cascadeSourceModifierId: null,
        changedFields: ['compatibility'],
      },
    ],
    nextCursor: null,
  })),
  fetchModifierVersion: vi.fn(async (_modifierId: string, revision: number) => ({
    versionId,
    modifierId,
    revision,
    name: revision === 1 ? 'Первая редакция' : 'Архивная редакция',
    description: 'Сохранённое описание',
    category: 'round',
    iconEmoji: '🧭',
    activationCommand: '!архив',
    activationCost: revision === 1 ? 4 : 9,
    activationLimit: { count: 2 },
    normalizedTags: ['история'],
    behaviorV2: {
      schemaVersion: 2,
      kind: 'rule',
      phase: 'round',
      performer: 'activeTeam',
      requiresHostMonitoring: false,
      rule: 'Неизменяемое правило',
      stackingPolicy: 'aggregateParameters',
      resolution: { type: 'ruleStatus' },
      reward: 'none',
      formulaReference: null,
    },
    conflicts:
      revision === 1
        ? []
        : [{ modifierId: '40000000-0000-0000-0000-000000000001', name: 'Конфликт-снимок' }],
    createdAtUtc: '2026-09-01T09:00:00Z',
    createdByUserId: null,
    createdByDisplayName: 'Администратор',
    changeNote: '<img src=x onerror=alert(1)>',
    changeType: 'compatibility_cascade',
    cascadeSourceModifierId: '50000000-0000-0000-0000-000000000001',
    changedFields: revision === 1 ? ['created'] : ['compatibility', 'activationCost'],
    isCurrent: revision === 2,
    isArchived: true,
  })),
  fetchModifierVersionGames: vi.fn(async () => ({
    items: [
      {
        gameId,
        gameTitle: 'Игра со второй редакцией',
        gameStatus: 'finished',
        startedAtUtc: '2026-09-01T10:00:00Z',
        finishedAtUtc: '2026-09-01T12:00:00Z',
        successfulActivationsCount: 0,
        cancelledActivationsCount: 1,
        resultsCount: 1,
        isEmergencyDisabled: true,
      },
    ],
    nextCursor: null,
  })),
}))

describe('ModifierHistoryPage', () => {
  it('renders archived cascade detail, semantic diff and related game without mutation controls', async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    renderWithAppProviders(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter
          initialEntries={[`/panel/modifier-history?modifierId=${modifierId}&revision=2`]}
        >
          <ModifierHistoryPage />
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(await screen.findByText('Каскад совместимости')).toBeInTheDocument()
    expect(screen.getAllByText('В архиве')).toHaveLength(2)
    expect(screen.getByText('Совместимость')).toBeInTheDocument()
    expect(screen.getByText(/Было: Конфликтов нет/)).toBeInTheDocument()
    expect(screen.getByText(/Стало: Конфликт-снимок/)).toBeInTheDocument()
    expect(screen.getByText('Неизменяемое правило')).toBeInTheDocument()
    expect(screen.getByText('<img src=x onerror=alert(1)>')).toBeInTheDocument()
    expect(document.querySelector('img[src="x"]')).toBeNull()
    expect(screen.getByText('Конфликт-снимок')).toBeInTheDocument()
    expect(screen.getByText('Аварийно отключён')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Игра со второй редакцией' })).toHaveAttribute(
      'href',
      `/panel/game-history?gameId=${gameId}`,
    )
    expect(screen.queryByRole('button', { name: /редактировать|удалить/i })).not.toBeInTheDocument()
  })
})
