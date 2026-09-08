import { getBackendOrigin } from './config.ts'

export function resolveBackendMediaUrl(url: string | null | undefined) {
  const candidate = url?.trim()
  if (!candidate || candidate.startsWith('//')) {
    return ''
  }

  try {
    const resolvedUrl = new URL(candidate, `${getBackendOrigin()}/`)
    return resolvedUrl.protocol === 'http:' || resolvedUrl.protocol === 'https:'
      ? resolvedUrl.toString()
      : ''
  } catch {
    return ''
  }
}
