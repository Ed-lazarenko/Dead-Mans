import { cleanup, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { ParticipantNamesList } from './ParticipantNamesList.tsx'

afterEach(cleanup)

describe('ParticipantNamesList', () => {
  it('renders every participant on a separate list row', () => {
    renderWithAppProviders(
      <ParticipantNamesList names={['Player One', 'Player Two']} emptyLabel="No players" />,
    )

    const list = screen.getByRole('list')
    expect(within(list).getAllByRole('listitem')).toHaveLength(2)
    expect(within(list).getByText('Player One')).toBeInTheDocument()
    expect(within(list).getByText('Player Two')).toBeInTheDocument()
    expect(screen.queryByText('Player One, Player Two')).not.toBeInTheDocument()
  })

  it('renders the empty label without an empty list', () => {
    renderWithAppProviders(<ParticipantNamesList names={[]} emptyLabel="No players" />)

    expect(screen.getByText('No players')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })

  it('can arrange participants horizontally', () => {
    renderWithAppProviders(
      <ParticipantNamesList
        names={['Player One', 'Player Two']}
        emptyLabel="No players"
        direction="row"
      />,
    )

    expect(screen.getByRole('list')).toHaveStyle({
      flexDirection: 'row',
      flexWrap: 'wrap',
      justifyContent: 'center',
    })
  })
})
