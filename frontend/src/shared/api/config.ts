const DEFAULT_API_MODE = 'mock'
const DEFAULT_API_BASE_URL = '/api'
const DEFAULT_BACKEND_ORIGIN = 'http://localhost:5285'

export function isHttpApiMode() {
  return (import.meta.env.VITE_API_MODE ?? DEFAULT_API_MODE) === 'http'
}

export function getApiBaseUrl() {
  return import.meta.env.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL
}

export function getBackendOrigin() {
  return import.meta.env.VITE_BACKEND_ORIGIN ?? DEFAULT_BACKEND_ORIGIN
}
