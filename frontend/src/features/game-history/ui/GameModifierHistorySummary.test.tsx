import { screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeAll, describe, expect, it } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { GameModifierHistorySummary } from './GameModifierHistorySummary.tsx'

type Snapshot = components['schemas']['GameHistoryModifierSnapshotDto']

beforeAll(async () => i18n.changeLanguage('ru'))

function snapshot(overrides: Partial<Snapshot>): Snapshot {
  return {
    modifierId: crypto.randomUUID(),
    versionId: crypto.randomUUID(),
    revision: 2,
    name: 'Исторический модификатор',
    description: 'Полная закреплённая конфигурация',
    category: 'round',
    iconEmoji: null,
    activationCommand: '!история',
    activationCost: 7,
    activationLimit: { count: 2 },
    normalizedTags: ['история'],
    behaviorV2: {
      schemaVersion: 2,
      kind: 'rule',
      phase: 'round',
      performer: 'activeTeam',
      requiresHostMonitoring: false,
      rule: 'Закреплённое правило',
      stackingPolicy: 'aggregateParameters',
      resolution: { type: 'ruleStatus' },
      reward: 'none',
      formulaReference: null,
    },
    conflicts: [],
    successfulActivationsCount: 0,
    cancelledActivationsCount: 0,
    resultsCount: 0,
    isEmergencyDisabled: false,
    emergencyDisabledAtUtc: null,
    ...overrides,
  }
}

describe('GameModifierHistorySummary', () => {
  it('shows the complete pinned set including unused, cancelled and emergency-disabled entries', () => {
    renderWithAppProviders(
      <MemoryRouter>
        <GameModifierHistorySummary
          rounds={[]}
          snapshots={[
            snapshot({ name: 'Не использован' }),
            snapshot({
              name: 'Отменён и отключён',
              successfulActivationsCount: 0,
              cancelledActivationsCount: 1,
              resultsCount: 1,
              isEmergencyDisabled: true,
            }),
          ]}
        />
      </MemoryRouter>,
    )

    expect(screen.getByText('Не использован · Редакция 2')).toBeInTheDocument()
    expect(screen.getByText('Отменён и отключён · Редакция 2')).toBeInTheDocument()
    expect(screen.getAllByText('Не активирован')).toHaveLength(2)
    expect(screen.getByText('Отменено: 1')).toBeInTheDocument()
    expect(screen.getByText('Результатов: 1')).toBeInTheDocument()
    expect(screen.getByText('Аварийно отключён')).toBeInTheDocument()
  })

  it('shows the legacy warning only when revision snapshots are unavailable', () => {
    renderWithAppProviders(
      <MemoryRouter>
        <GameModifierHistorySummary rounds={[]} snapshotStatus="legacy_unavailable" />
      </MemoryRouter>,
    )
    expect(screen.getByText(/недостающие редакции не восстанавливаются/i)).toBeInTheDocument()
  })
})
