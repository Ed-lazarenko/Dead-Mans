import { Box, CircularProgress, Stack, Typography } from '@mui/material'
import { useId, useRef, useState, type DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton } from '../../../shared/ui/index.ts'
import {
  createSetupCellDropzoneSx,
  setupCellBusyOverlaySx,
  setupCellDragOverlaySx,
  setupCellImageLabelSx,
  setupCellImagePreviewSx,
  setupCellMediaActionsSx,
} from '../theme/cell-image-sx.ts'
import {
  dataTransferHasImageFiles,
  extractGameSetupCellMediaFileFromDataTransfer,
  GAME_SETUP_CELL_MEDIA_ALLOWED_MIME_TYPES,
} from '../model/game-setup-cell-media-limits.ts'
import type { GameSetupCellMediaPhase } from '../model/game-setup-cell-media-display.ts'

interface GameSetupCellImageProps {
  imageUrl: string | undefined
  imageKey: string | undefined
  alt: string
  cellId: string | undefined
  phase: GameSetupCellMediaPhase
  canManageMedia: boolean
  isBusy: boolean
  onUpload: (cellId: string | undefined, file: File) => void
  onDelete: (cellId: string | undefined) => void
}

interface GameSetupCellImagePreviewProps {
  imageUrl: string
  imageKey: string | undefined
  alt: string
  isBusy: boolean
  isDragOver: boolean
}

function GameSetupCellImagePreview({
  imageUrl,
  imageKey,
  alt,
  isBusy,
  isDragOver,
}: GameSetupCellImagePreviewProps) {
  const [imageLoaded, setImageLoaded] = useState(false)

  return (
    <Box
      component="img"
      key={imageKey ?? imageUrl}
      src={imageUrl}
      alt={alt}
      onLoad={() => setImageLoaded(true)}
      onError={() => setImageLoaded(false)}
      sx={[
        setupCellImagePreviewSx,
        {
          opacity: imageLoaded && !isBusy && !isDragOver ? 1 : 0,
        },
      ]}
    />
  )
}

export function GameSetupCellImage({
  imageUrl,
  imageKey,
  alt,
  cellId,
  phase,
  canManageMedia,
  isBusy,
  onUpload,
  onDelete,
}: GameSetupCellImageProps) {
  const { t } = useTranslation()
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const dragDepthRef = useRef(0)
  const [isDragOver, setIsDragOver] = useState(false)

  const canAcceptDrop = canManageMedia && !isBusy
  const showDragOver = canAcceptDrop && isDragOver

  const openFilePicker = () => {
    if (!canManageMedia || isBusy) {
      return
    }

    inputRef.current?.click()
  }

  const submitFile = (file: File) => {
    onUpload(cellId, file)
  }

  const resetDragState = () => {
    dragDepthRef.current = 0
    setIsDragOver(false)
  }

  const handleDragEnter = (event: DragEvent<HTMLElement>) => {
    if (!canAcceptDrop || !dataTransferHasImageFiles(event.dataTransfer)) {
      return
    }

    event.preventDefault()
    event.stopPropagation()
    dragDepthRef.current += 1
    setIsDragOver(true)
  }

  const handleDragLeave = (event: DragEvent<HTMLElement>) => {
    if (!canAcceptDrop) {
      return
    }

    event.preventDefault()
    event.stopPropagation()
    dragDepthRef.current = Math.max(0, dragDepthRef.current - 1)
    if (dragDepthRef.current === 0) {
      setIsDragOver(false)
    }
  }

  const handleDragOver = (event: DragEvent<HTMLElement>) => {
    if (!canAcceptDrop || !dataTransferHasImageFiles(event.dataTransfer)) {
      return
    }

    event.preventDefault()
    event.stopPropagation()
    event.dataTransfer.dropEffect = 'copy'
  }

  const handleDrop = (event: DragEvent<HTMLElement>) => {
    if (!canAcceptDrop) {
      return
    }

    event.preventDefault()
    event.stopPropagation()
    resetDragState()

    const file = extractGameSetupCellMediaFileFromDataTransfer(event.dataTransfer)
    if (!file) {
      return
    }

    submitFile(file)
  }

  const showImage = Boolean(imageUrl)
  const statusLabel =
    phase === 'uploading'
      ? t('gameSetup.cellMedia.uploading')
      : phase === 'deleting'
        ? t('gameSetup.cellMedia.removing')
        : null

  return (
    <Box
      onDragEnter={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
      sx={createSetupCellDropzoneSx({ showDragOver })}
    >
      <Box
        component="label"
        htmlFor={canManageMedia && !isBusy ? inputId : undefined}
        sx={[
          setupCellImageLabelSx,
          {
            cursor: canManageMedia && !isBusy ? 'pointer' : 'default',
          },
        ]}
        onClick={(event) => {
          if ((event.target as HTMLElement).closest('[data-cell-media-action]')) {
            event.preventDefault()
          }
        }}
      >
        {showImage && imageUrl ? (
          <GameSetupCellImagePreview
            imageUrl={imageUrl}
            imageKey={imageKey}
            alt={alt}
            isBusy={isBusy}
            isDragOver={showDragOver}
          />
        ) : (
          <>
            <Typography variant="caption" sx={{ px: 1 }}>
              {canManageMedia && !isBusy
                ? t('gameSetup.cellMedia.uploadPrompt')
                : t('gameSetup.imagePlaceholder')}
            </Typography>

            {canManageMedia ? (
              <Stack
                direction="row"
                spacing={0.5}
                justifyContent="center"
                sx={{ position: 'relative', zIndex: 4 }}
                data-cell-media-action
              >
                <AppButton
                  size="small"
                  disabled={isBusy}
                  onClick={openFilePicker}
                  data-cell-media-action
                >
                  {t('gameSetup.cellMedia.upload')}
                </AppButton>
              </Stack>
            ) : null}
          </>
        )}

        {showDragOver ? (
          <Box sx={setupCellDragOverlaySx}>
            <Typography variant="caption" sx={{ px: 1, color: 'primary.main', fontWeight: 600 }}>
              {t('gameSetup.cellMedia.dropPrompt')}
            </Typography>
          </Box>
        ) : null}

        {isBusy ? (
          <Box sx={setupCellBusyOverlaySx}>
            <CircularProgress size={28} color="inherit" />
            {statusLabel ? (
              <Typography variant="caption" sx={{ color: 'common.white', px: 1 }}>
                {statusLabel}
              </Typography>
            ) : null}
          </Box>
        ) : null}
      </Box>

      {canManageMedia && showImage ? (
        <Stack
          direction="row"
          spacing={0.5}
          justifyContent="center"
          sx={setupCellMediaActionsSx}
          data-cell-media-action
        >
          <AppButton size="small" disabled={isBusy} onClick={openFilePicker} data-cell-media-action>
            {showImage && phase !== 'deleting'
              ? t('gameSetup.cellMedia.replace')
              : t('gameSetup.cellMedia.upload')}
          </AppButton>
          {showImage && phase !== 'deleting' ? (
            <AppButton
              size="small"
              tone="ghost"
              disabled={isBusy}
              onClick={() => onDelete(cellId)}
              data-cell-media-action
            >
              {t('common.actions.remove')}
            </AppButton>
          ) : null}
        </Stack>
      ) : null}

      <input
        ref={inputRef}
        id={inputId}
        type="file"
        hidden
        accept={GAME_SETUP_CELL_MEDIA_ALLOWED_MIME_TYPES.join(',')}
        onChange={(event) => {
          const file = event.target.files?.[0]
          event.target.value = ''
          if (!file) {
            return
          }

          submitFile(file)
        }}
      />
    </Box>
  )
}
