import { describe, expect, it, vi } from 'vitest'

vi.mock('./config.ts', () => ({
  getBackendOrigin: () => 'http://localhost:5285',
}))

describe('resolveBackendMediaUrl', () => {
  it('returns an absolute URL unchanged', async () => {
    const { resolveBackendMediaUrl } = await import('./media-url.ts')

    expect(resolveBackendMediaUrl('https://cdn.example.com/cards/card-1.png')).toBe(
      'https://cdn.example.com/cards/card-1.png',
    )
  })

  it('resolves a relative backend media URL against the backend origin', async () => {
    const { resolveBackendMediaUrl } = await import('./media-url.ts')

    expect(resolveBackendMediaUrl('/media/cards/card-1.png')).toBe(
      'http://localhost:5285/media/cards/card-1.png',
    )
  })

  it('returns an empty string when the URL is missing', async () => {
    const { resolveBackendMediaUrl } = await import('./media-url.ts')

    expect(resolveBackendMediaUrl(undefined)).toBe('')
    expect(resolveBackendMediaUrl(null)).toBe('')
    expect(resolveBackendMediaUrl('')).toBe('')
  })
})
