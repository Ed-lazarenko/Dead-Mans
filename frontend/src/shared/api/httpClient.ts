import { logger } from '../lib/logger.ts'

const DEFAULT_BASE_URL = '/api'

function getBaseUrl() {
  return import.meta.env.VITE_API_BASE_URL ?? DEFAULT_BASE_URL
}

export interface HttpRequestOptions extends RequestInit {
  query?: Record<string, string | number | boolean | undefined>
}

async function handleResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get('content-type') ?? ''
  const isJson = contentType.includes('application/json')

  if (!response.ok) {
    const errorBody = isJson ? await response.json().catch(() => undefined) : await response.text().catch(() => '')
    logger.error('HTTP error', { status: response.status, url: response.url, body: errorBody })
    throw new Error(`HTTP ${response.status}`)
  }

  if (!isJson) {
    return (await response.text()) as T
  }

  return (await response.json()) as T
}

function buildUrl(path: string, query?: HttpRequestOptions['query']) {
  const base = getBaseUrl().replace(/\/$/, '')
  const cleanPath = path.startsWith('/') ? path : `/${path}`
  const url = new URL(`${base}${cleanPath}`, window.location.origin)

  if (query) {
    Object.entries(query).forEach(([key, value]) => {
      if (value === undefined) return
      url.searchParams.set(key, String(value))
    })
  }

  return url.toString()
}

export const httpClient = {
  async get<T>(path: string, options: HttpRequestOptions = {}): Promise<T> {
    const url = buildUrl(path, options.query)
    logger.debug('HTTP GET', { url, options })
    const response = await fetch(url, {
      ...options,
      method: 'GET',
    })
    return handleResponse<T>(response)
  },

  async post<T>(path: string, body?: unknown, options: HttpRequestOptions = {}): Promise<T> {
    const url = buildUrl(path, options.query)
    logger.debug('HTTP POST', { url, body, options })
    const response = await fetch(url, {
      ...options,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(options.headers ?? {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
    return handleResponse<T>(response)
  },
}


