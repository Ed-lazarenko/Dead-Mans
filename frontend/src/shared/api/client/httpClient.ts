import { logger } from '../../lib/logger.ts'
import { ApiError } from '../errors/ApiError.ts'
import { getApiBaseUrl } from '../config.ts'

export interface HttpRequestOptions extends RequestInit {
  query?: Record<string, string | number | boolean | undefined>
  baseUrl?: string
}

async function handleResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get('content-type') ?? ''
  const isJson = contentType.includes('application/json')

  if (!response.ok) {
    const errorBody = isJson ? await response.json().catch(() => undefined) : await response.text().catch(() => '')
    logger.error('HTTP error', { status: response.status, url: response.url, body: errorBody })
    throw new ApiError(`HTTP ${response.status}`, {
      status: response.status,
      details: errorBody,
    })
  }

  if (!isJson) {
    return (await response.text()) as T
  }

  return (await response.json()) as T
}

function buildUrl(path: string, options: HttpRequestOptions = {}) {
  const base = (options.baseUrl ?? getApiBaseUrl()).replace(/\/$/, '')
  const cleanPath = path.startsWith('/') ? path : `/${path}`
  const url = new URL(`${base}${cleanPath}`, window.location.origin)

  if (options.query) {
    Object.entries(options.query).forEach(([key, value]) => {
      if (value === undefined) return
      url.searchParams.set(key, String(value))
    })
  }

  return url.toString()
}

function toRequestInit(options: HttpRequestOptions): RequestInit {
  const { query, baseUrl, ...requestInit } = options
  void query
  void baseUrl
  return {
    credentials: 'include',
    ...requestInit,
  }
}

export const httpClient = {
  async get<T>(path: string, options: HttpRequestOptions = {}): Promise<T> {
    const url = buildUrl(path, options)
    logger.debug('HTTP GET', { url, options })
    const requestInit = toRequestInit(options)
    const response = await fetch(url, {
      ...requestInit,
      method: 'GET',
    })
    return handleResponse<T>(response)
  },

  async post<T>(path: string, body?: unknown, options: HttpRequestOptions = {}): Promise<T> {
    const url = buildUrl(path, options)
    logger.debug('HTTP POST', { url, body, options })
    const requestInit = toRequestInit(options)
    const response = await fetch(url, {
      ...requestInit,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(requestInit.headers ?? {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
    return handleResponse<T>(response)
  },
}
