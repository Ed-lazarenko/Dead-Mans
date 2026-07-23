import { getBackendOrigin } from './config.ts'

export function resolveBackendMediaUrl(url: string | null | undefined) {
  if (!url) {
    return ''
  }

  try {
    return new URL(url).toString()
  } catch {
    return new URL(url, `${getBackendOrigin()}/`).toString()
  }
}
