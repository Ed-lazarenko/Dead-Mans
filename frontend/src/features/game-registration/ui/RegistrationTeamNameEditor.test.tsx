import { cleanup, fireEvent, screen } from '@testing-library/react'
import { useState } from 'react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { RegistrationTeamNameEditor } from './RegistrationTeamNameEditor.tsx'

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('RegistrationTeamNameEditor', () => {
  it('normalizes whitespace before saving', () => {
    const onSave = vi.fn()
    renderWithAppProviders(
      <RegistrationTeamNameEditor value={null} canEdit isSaving={false} onSave={onSave} />,
    )

    fireEvent.change(screen.getByLabelText('Название команды'), {
      target: { value: '  Night   Watch  ' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    expect(onSave).toHaveBeenCalledWith('Night Watch')
  })

  it('keeps save disabled until the value actually changes', () => {
    const onSave = vi.fn()
    renderWithAppProviders(
      <RegistrationTeamNameEditor value="Night Watch" canEdit isSaving={false} onSave={onSave} />,
    )

    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeDisabled()
  })

  it('syncs the field when the external value changes', () => {
    const onSave = vi.fn()

    function Host() {
      const [value, setValue] = useState('Old Name')

      return (
        <>
          <button type="button" onClick={() => setValue('New Name')}>
            external update
          </button>
          <RegistrationTeamNameEditor value={value} canEdit isSaving={false} onSave={onSave} />
        </>
      )
    }

    renderWithAppProviders(<Host />)
    fireEvent.click(screen.getByRole('button', { name: 'external update' }))

    expect(screen.getByLabelText('Название команды')).toHaveValue('New Name')
  })
})
