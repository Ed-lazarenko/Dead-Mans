import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('application shell security', () => {
  it('does not load executable, framed, or media resources from external origins', () => {
    const html = readFileSync(resolve(process.cwd(), 'index.html'), 'utf8')
    const externalResourceTags =
      html.match(
        /<(?:audio|iframe|img|link|script|source|video)\b[^>]*(?:href|src)=["']https?:\/\//giu,
      ) ?? []

    expect(externalResourceTags).toEqual([])
  })
})
