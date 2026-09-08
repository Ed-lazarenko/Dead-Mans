import { afterEach, describe, expect, it, vi } from 'vitest'
import { getApiBaseUrl, getBackendOrigin } from './config.ts'

afterEach(() => {
  vi.unstubAllEnvs()
})

describe('frontend endpoint configuration', () => {
  it('accepts a same-origin API path and removes its trailing slash', () => {
    vi.stubEnv('VITE_API_BASE_URL', '/api/')

    expect(getApiBaseUrl()).toBe('/api')
  })

  it('rejects insecure absolute API URLs outside development', () => {
    vi.stubEnv('DEV', false)
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.example.com/api')

    expect(() => getApiBaseUrl()).toThrow('must use HTTPS')
  })

  it('rejects protocol-relative API URLs', () => {
    vi.stubEnv('VITE_API_BASE_URL', '//attacker.example/api')

    expect(() => getApiBaseUrl()).toThrow('absolute http/https URL')
  })

  it('requires a clean HTTPS backend origin outside development', () => {
    vi.stubEnv('DEV', false)
    vi.stubEnv('VITE_BACKEND_ORIGIN', 'https://api.example.com/path')

    expect(() => getBackendOrigin()).toThrow('without path')

    vi.stubEnv('VITE_BACKEND_ORIGIN', 'http://api.example.com')
    expect(() => getBackendOrigin()).toThrow('must use HTTPS')

    vi.stubEnv('VITE_BACKEND_ORIGIN', 'https://api.example.com/')
    expect(getBackendOrigin()).toBe('https://api.example.com')
  })
})
