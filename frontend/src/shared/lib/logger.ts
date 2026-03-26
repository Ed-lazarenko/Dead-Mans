type LogLevel = 'debug' | 'info' | 'warn' | 'error'

// Keep debug logs in development only.
const isDev = import.meta.env.DEV

function log(level: LogLevel, message: string, payload?: unknown) {
  if (!isDev && level === 'debug') return

  const prefix = `[DeadMans][${level.toUpperCase()}]`

  if (payload !== undefined) {
    console[level](`${prefix} ${message}`, payload)
  } else {
    console[level](`${prefix} ${message}`)
  }
}

export const logger = {
  debug: (message: string, payload?: unknown) => log('debug', message, payload),
  info: (message: string, payload?: unknown) => log('info', message, payload),
  warn: (message: string, payload?: unknown) => log('warn', message, payload),
  error: (message: string, payload?: unknown) => log('error', message, payload),
}


