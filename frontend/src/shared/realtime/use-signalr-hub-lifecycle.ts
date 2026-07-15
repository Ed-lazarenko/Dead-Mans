import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { logger } from '../lib/logger.ts'
import { buildRealtimeHubUrl } from './hub-url.ts'
import type { RealtimeHubKey } from './generated.ts'
import { isExpectedSignalrNegotiationShutdown } from './signalr-connection.ts'

const INITIAL_CONNECT_RETRY_DELAY_MS = 3000

interface UseSignalrHubLifecycleOptions {
  hub: RealtimeHubKey
  logLabel: string
  onConnected: () => Promise<void> | void
  registerEventHandlers: (connection: HubConnection) => () => void
}

export function useSignalrHubLifecycle({
  hub,
  logLabel,
  onConnected,
  registerEventHandlers,
}: UseSignalrHubLifecycleOptions) {
  useEffect(() => {
    let disposed = false
    let initialRetryTimer: ReturnType<typeof setTimeout> | null = null
    const connection = new HubConnectionBuilder()
      .withUrl(buildRealtimeHubUrl(hub), { withCredentials: true })
      .withAutomaticReconnect()
      .build()
    const unregisterEventHandlers = registerEventHandlers(connection)

    const clearInitialRetryTimer = () => {
      if (initialRetryTimer !== null) {
        clearTimeout(initialRetryTimer)
        initialRetryTimer = null
      }
    }

    const runConnectedSync = async (reason: 'connected' | 'reconnected') => {
      try {
        await onConnected()
      } catch (error) {
        logger.warn(`${logLabel} realtime ${reason} resync failed`, error)
      }
    }

    const scheduleInitialRetry = () => {
      if (disposed || initialRetryTimer !== null) {
        return
      }

      initialRetryTimer = setTimeout(() => {
        initialRetryTimer = null
        void startConnection()
      }, INITIAL_CONNECT_RETRY_DELAY_MS)
    }

    const startConnection = async () => {
      try {
        await connection.start()
        clearInitialRetryTimer()

        if (disposed) {
          await connection.stop()
          return
        }

        logger.info(`${logLabel} realtime connected`)
        await runConnectedSync('connected')
      } catch (error) {
        if (disposed || isExpectedSignalrNegotiationShutdown(error)) {
          return
        }

        logger.error(`${logLabel} realtime failed to start`, error)
        scheduleInitialRetry()
      }
    }

    connection.onreconnecting((error) => {
      logger.warn(`${logLabel} realtime reconnecting`, error)
    })

    connection.onreconnected(async () => {
      logger.info(`${logLabel} realtime reconnected`)
      await runConnectedSync('reconnected')
    })

    connection.onclose((error) => {
      if (!disposed && error) {
        logger.warn(`${logLabel} realtime connection closed`, error)
      }
    })

    const startPromise = startConnection()

    return () => {
      disposed = true
      clearInitialRetryTimer()
      unregisterEventHandlers()
      void (async () => {
        await startPromise.catch(() => undefined)
        if (connection.state !== HubConnectionState.Disconnected) {
          await connection.stop()
        }
      })()
    }
  }, [hub, logLabel, onConnected, registerEventHandlers])
}
