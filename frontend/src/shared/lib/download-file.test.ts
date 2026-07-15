import { afterEach, describe, expect, it, vi } from 'vitest'
import { downloadBlobFile, downloadTextFile } from './download-file.ts'

describe('download-file', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('downloads a blob and revokes the temporary object URL', () => {
    const createObjectURL = vi.fn(() => 'blob:test')
    const revokeObjectURL = vi.fn()
    const click = vi.fn()
    const append = vi.spyOn(document.body, 'append')
    const remove = vi.fn()
    const originalCreateElement = document.createElement.bind(document)
    const createElement = vi.spyOn(document, 'createElement').mockImplementation((tagName) => {
      if (tagName !== 'a') {
        return originalCreateElement(tagName)
      }

      return {
        click,
        remove,
        set href(value: string) {
          this._href = value
        },
        get href() {
          return this._href
        },
        set download(value: string) {
          this._download = value
        },
        get download() {
          return this._download
        },
      } as unknown as HTMLAnchorElement
    })

    Object.defineProperty(URL, 'createObjectURL', { value: createObjectURL, configurable: true })
    Object.defineProperty(URL, 'revokeObjectURL', { value: revokeObjectURL, configurable: true })

    downloadBlobFile(new Blob(['hello']), 'hello.txt')

    expect(createObjectURL).toHaveBeenCalledTimes(1)
    expect(append).toHaveBeenCalledTimes(1)
    expect(click).toHaveBeenCalledTimes(1)
    expect(remove).toHaveBeenCalledTimes(1)
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:test')

    createElement.mockRestore()
  })

  it('downloads text content with the provided mime type', async () => {
    let savedBlob: Blob | null = null
    Object.defineProperty(URL, 'createObjectURL', {
      value: vi.fn((blob: Blob) => {
        savedBlob = blob
        return 'blob:text'
      }),
      configurable: true,
    })
    Object.defineProperty(URL, 'revokeObjectURL', { value: vi.fn(), configurable: true })
    vi.spyOn(document.body, 'append').mockImplementation(() => document.body)
    vi.spyOn(document, 'createElement').mockReturnValue({
      click: vi.fn(),
      remove: vi.fn(),
    } as unknown as HTMLAnchorElement)

    downloadTextFile('payload', 'payload.json', 'application/json')

    expect(savedBlob).not.toBeNull()
    expect(savedBlob!.type).toBe('application/json')
    expect(await savedBlob!.text()).toBe('payload')
  })
})
