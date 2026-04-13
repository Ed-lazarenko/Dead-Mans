const DEFAULT_API_BASE_URL = '/api'
const DEFAULT_BACKEND_ORIGIN = 'http://localhost:5285'

export function getApiBaseUrl() {
  return import.meta.env.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL
}

export function getBackendOrigin() {
  const configuredOrigin = import.meta.env.VITE_BACKEND_ORIGIN?.trim()
  if (configuredOrigin) {
    return ensureValidOrigin(configuredOrigin, 'VITE_BACKEND_ORIGIN')
  }

  if (import.meta.env.DEV) {
    return DEFAULT_BACKEND_ORIGIN
  }

  if (typeof window !== 'undefined' && window.location.origin) {
    return ensureValidOrigin(window.location.origin, 'window.location.origin')
  }

  throw new Error(
    'VITE_BACKEND_ORIGIN is required outside development when window.location.origin is unavailable.'
  )
}

function ensureValidOrigin(value: string, sourceName: string) {
  let url: URL
  try {
    url = new URL(value)
  } catch {
    throw new Error(`${sourceName} must be an absolute URL origin (for example, https://api.example.com).`)
  }

  if ((url.protocol !== 'http:' && url.protocol !== 'https:') || !url.host) {
    throw new Error(`${sourceName} must use http/https and include host (and optional port).`)
  }

  return url.origin
}
