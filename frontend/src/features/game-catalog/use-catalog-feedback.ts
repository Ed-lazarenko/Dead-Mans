import { useCallback, useState } from 'react'
import type { TFunction } from 'i18next'
import { resolveCatalogErrorMessage } from './model/catalog-error.ts'

export function useCatalogFeedback(t: TFunction) {
  const [listError, setListError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const clearListError = useCallback(() => {
    setListError(null)
  }, [])

  const clearSuccessMessage = useCallback(() => {
    setSuccessMessage(null)
  }, [])

  const resetFeedback = useCallback(() => {
    setListError(null)
    setSuccessMessage(null)
  }, [])

  const showResolvedError = useCallback(
    (error: unknown) => {
      setListError(resolveCatalogErrorMessage(error, t))
    },
    [t],
  )

  return {
    listError,
    successMessage,
    setSuccessMessage,
    clearListError,
    clearSuccessMessage,
    resetFeedback,
    showResolvedError,
  }
}
