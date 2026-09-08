const DEFAULT_API_BASE_URL = '/api'
const DEFAULT_BACKEND_ORIGIN = 'http://localhost:5285'

export function getApiBaseUrl() {
  const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
  if (!configuredBaseUrl) {
    return DEFAULT_API_BASE_URL
  }

  return ensureValidApiBaseUrl(configuredBaseUrl, !import.meta.env.DEV)
}

export function getBackendOrigin() {
  const configuredOrigin = import.meta.env.VITE_BACKEND_ORIGIN?.trim()
  if (configuredOrigin) {
    return ensureValidOrigin(configuredOrigin, 'VITE_BACKEND_ORIGIN', !import.meta.env.DEV)
  }

  if (import.meta.env.DEV) {
    return DEFAULT_BACKEND_ORIGIN
  }

  if (typeof window !== 'undefined' && window.location.origin) {
    return ensureValidOrigin(window.location.origin, 'window.location.origin', true)
  }

  throw new Error(
    'VITE_BACKEND_ORIGIN is required outside development when window.location.origin is unavailable.',
  )
}

function ensureValidApiBaseUrl(value: string, requireHttps: boolean) {
  if (value.startsWith('/') && !value.startsWith('//')) {
    const url = new URL(value, 'https://configuration.invalid')
    if (url.search || url.hash) {
      throw new Error('VITE_API_BASE_URL must not contain a query string or fragment.')
    }

    return url.pathname.replace(/\/$/, '') || '/'
  }

  const url = parseHttpUrl(value, 'VITE_API_BASE_URL')
  if (url.search || url.hash) {
    throw new Error('VITE_API_BASE_URL must not contain a query string or fragment.')
  }
  if (requireHttps && url.protocol !== 'https:') {
    throw new Error('VITE_API_BASE_URL must use HTTPS outside development.')
  }

  return url.toString().replace(/\/$/, '')
}

function ensureValidOrigin(value: string, sourceName: string, requireHttps: boolean) {
  const url = parseHttpUrl(value, sourceName)
  if (url.pathname !== '/' || url.search || url.hash) {
    throw new Error(`${sourceName} must be an origin without path, query string, or fragment.`)
  }
  if (requireHttps && url.protocol !== 'https:') {
    throw new Error(`${sourceName} must use HTTPS outside development.`)
  }

  return url.origin
}

function parseHttpUrl(value: string, sourceName: string) {
  let url: URL
  try {
    url = new URL(value)
  } catch {
    throw new Error(`${sourceName} must be an absolute http/https URL.`)
  }

  if ((url.protocol !== 'http:' && url.protocol !== 'https:') || !url.host) {
    throw new Error(`${sourceName} must use http/https and include host (and optional port).`)
  }

  if (url.username || url.password) {
    throw new Error(`${sourceName} must not contain credentials.`)
  }

  return url
}
