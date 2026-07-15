import createClient from 'openapi-fetch'
import { getApiBaseUrl, getBackendOrigin } from '../config.ts'
import { ApiError } from '../errors/ApiError.ts'
import { logger } from '../../lib/logger.ts'

type OpenApiSuccess<TData> = {
  data: TData
  error?: never
  response: Response
}

type OpenApiResult<TData> =
  | OpenApiSuccess<TData>
  | {
      error: unknown
      response: Response
    }

function createOpenApiClient<TPaths extends object>(baseUrl: string) {
  return createClient<TPaths>({
    baseUrl,
    credentials: 'include',
    headers: {
      'X-Dead-Mans-Api-Client': '1',
    },
  })
}

export function createApiClient<TPaths extends object>() {
  return createOpenApiClient<TPaths>(getApiBaseUrl())
}

export function createBackendApiClient<TPaths extends object>() {
  return createOpenApiClient<TPaths>(getBackendOrigin())
}

function throwOpenApiError<TData>(
  result: OpenApiResult<TData>,
): asserts result is OpenApiSuccess<TData> {
  if ('error' in result || !result.response.ok) {
    const details = 'error' in result ? result.error : undefined
    logger.error('HTTP error', {
      status: result.response.status,
      url: result.response.url,
      body: details,
    })
    throw new ApiError(`HTTP ${result.response.status}`, {
      status: result.response.status,
      details,
    })
  }
}

function isDefined<TValue>(value: TValue): value is Exclude<TValue, undefined> {
  return value !== undefined
}

export async function unwrapOpenApiData<TData>(
  request: Promise<OpenApiResult<TData>>,
): Promise<Exclude<TData, undefined>> {
  const result = await request
  throwOpenApiError(result)

  if (!isDefined(result.data)) {
    throw new ApiError('API returned an empty response where JSON data was expected', {
      status: result.response.status,
    })
  }

  return result.data
}

export async function unwrapOpenApiDataOrNullOn404<TData>(
  request: Promise<OpenApiResult<TData>>,
): Promise<Exclude<TData, undefined> | null> {
  const result = await request

  if (result.response.status === 404) {
    return null
  }

  throwOpenApiError(result)

  if (!isDefined(result.data)) {
    throw new ApiError('API returned an empty response where JSON data was expected', {
      status: result.response.status,
    })
  }

  return result.data
}

export async function unwrapOpenApiDataOrNullOn401<TData>(
  request: Promise<OpenApiResult<TData>>,
): Promise<Exclude<TData, undefined> | null> {
  const result = await request

  if (result.response.status === 401) {
    return null
  }

  throwOpenApiError(result)

  if (!isDefined(result.data)) {
    throw new ApiError('API returned an empty response where JSON data was expected', {
      status: result.response.status,
    })
  }

  return result.data
}

export async function unwrapOpenApiDataOrNullOnNoContent<TData>(
  request: Promise<OpenApiResult<TData>>,
): Promise<Exclude<TData, undefined> | null> {
  const result = await request

  if (result.response.status === 204) {
    return null
  }

  throwOpenApiError(result)

  if (!isDefined(result.data)) {
    throw new ApiError('API returned an empty response where JSON data was expected', {
      status: result.response.status,
    })
  }

  return result.data
}

export async function ensureOpenApiSuccess(
  request: Promise<OpenApiResult<unknown>>,
): Promise<void> {
  throwOpenApiError(await request)
}
