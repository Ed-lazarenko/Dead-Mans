import { Box, Drawer, IconButton, Menu, MenuItem, Stack, Tooltip, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton } from '../../../shared/ui/index.ts'

export interface AdminToolDescriptor {
  id: string
  label: string
  content: ReactNode
}

interface AdminToolDrawerProps {
  tools: readonly AdminToolDescriptor[]
  initialToolId: string
}

export function AdminToolDrawer({ tools, initialToolId }: AdminToolDrawerProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [activeToolId, setActiveToolId] = useState(initialToolId)
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)
  const availableToolIds = useMemo(() => tools.map((tool) => tool.id), [tools])
  const resolvedActiveToolId = availableToolIds.includes(activeToolId)
    ? activeToolId
    : availableToolIds.includes(initialToolId)
      ? initialToolId
      : availableToolIds[0]
  const activeToolIndex = tools.findIndex((tool) => tool.id === resolvedActiveToolId)
  const activeTool = tools[activeToolIndex] ?? null
  const hasMultipleTools = tools.length > 1
  const openerRef = useRef<HTMLButtonElement | null>(null)
  const shouldRestoreFocusRef = useRef(false)

  useEffect(() => {
    if (isOpen) {
      shouldRestoreFocusRef.current = true
      return
    }

    if (shouldRestoreFocusRef.current) {
      shouldRestoreFocusRef.current = false
      openerRef.current?.focus()
    }
  }, [isOpen])

  if (!activeTool) {
    return null
  }

  const selectRelativeTool = (offset: -1 | 1) => {
    if (!hasMultipleTools) return
    const nextIndex = (activeToolIndex + offset + tools.length) % tools.length
    setActiveToolId(tools[nextIndex].id)
  }

  return (
    <>
      <AppButton
        ref={openerRef}
        tone="secondary"
        size="medium"
        onClick={() => setIsOpen(true)}
        aria-haspopup="dialog"
        sx={(theme) => ({
          position: 'fixed',
          zIndex: theme.zIndex.drawer - 1,
          right: { xs: 12, md: 0 },
          top: { xs: 'auto', md: '50%' },
          bottom: { xs: 16, md: 'auto' },
          transform: { xs: 'none', md: 'translateY(-50%)' },
          minWidth: { xs: 0, md: 52 },
          minHeight: { xs: 46, md: 192 },
          px: { xs: 1.6, md: 0.95 },
          py: { xs: 0.9, md: 1.6 },
          borderRadius: { xs: 999, md: '18px 0 0 18px' },
          writingMode: { xs: 'horizontal-tb', md: 'vertical-rl' },
          textOrientation: { xs: 'mixed', md: 'mixed' },
          justifyContent: 'center',
          letterSpacing: '0.03em',
          whiteSpace: 'nowrap',
          boxShadow: `0 14px 28px ${alpha(theme.palette.common.black, 0.38)}`,
        })}
      >
        {t('adminTools.openAction')}
      </AppButton>

      <Drawer
        anchor="right"
        open={isOpen}
        onClose={() => setIsOpen(false)}
        ModalProps={{ keepMounted: true }}
        PaperProps={{
          sx: (theme) => ({
            width: { xs: '100vw', md: 520 },
            maxWidth: '100vw',
            height: '100dvh',
            display: 'grid',
            gridTemplateRows: 'auto minmax(0, 1fr)',
            borderLeft: `1px solid ${alpha(theme.palette.divider, 0.86)}`,
            backgroundImage: 'none',
            overflow: 'hidden',
          }),
        }}
      >
        <Box
          component="aside"
          aria-label={t('adminTools.drawerLabel')}
          sx={{ display: 'contents' }}
        >
          <Box
            sx={(theme) => ({
              borderBottom: `1px solid ${alpha(theme.palette.divider, 0.82)}`,
              backgroundColor: alpha(theme.palette.background.paper, 0.82),
              px: { xs: 1, sm: 1.25 },
              py: 1,
            })}
          >
            <Stack direction="row" spacing={0.5} alignItems="center">
              <Tooltip title={t('adminTools.previousTool')}>
                <span>
                  <IconButton
                    size="small"
                    disabled={!hasMultipleTools}
                    aria-label={t('adminTools.previousTool')}
                    onClick={() => selectRelativeTool(-1)}
                  >
                    <Box component="span" aria-hidden sx={{ fontSize: 22, lineHeight: 1 }}>
                      ←
                    </Box>
                  </IconButton>
                </span>
              </Tooltip>

              <AppButton
                tone="ghost"
                size="small"
                aria-label={`${activeTool.label}. ${t('adminTools.chooseTool')}`}
                aria-haspopup="menu"
                aria-expanded={menuAnchor !== null}
                aria-controls={menuAnchor ? 'admin-tool-menu' : undefined}
                onClick={(event) => setMenuAnchor(event.currentTarget)}
                sx={{ minWidth: 0, flex: 1, justifyContent: 'center', px: 1 }}
              >
                <Stack spacing={0.1} alignItems="center" sx={{ minWidth: 0 }}>
                  <Typography component="span" variant="subtitle1" fontWeight={850} noWrap>
                    {activeTool.label}
                  </Typography>
                  <Typography component="span" variant="caption" color="text.secondary">
                    {t('adminTools.toolPosition', {
                      current: activeToolIndex + 1,
                      total: tools.length,
                    })}
                  </Typography>
                </Stack>
              </AppButton>

              <Tooltip title={t('adminTools.nextTool')}>
                <span>
                  <IconButton
                    size="small"
                    disabled={!hasMultipleTools}
                    aria-label={t('adminTools.nextTool')}
                    onClick={() => selectRelativeTool(1)}
                  >
                    <Box component="span" aria-hidden sx={{ fontSize: 22, lineHeight: 1 }}>
                      →
                    </Box>
                  </IconButton>
                </span>
              </Tooltip>

              <Tooltip title={t('adminTools.closeAction')}>
                <IconButton
                  size="small"
                  aria-label={t('adminTools.closeAction')}
                  onClick={() => setIsOpen(false)}
                  sx={(theme) => ({
                    ml: 0.35,
                    border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
                  })}
                >
                  <Box component="span" aria-hidden sx={{ fontSize: 20, lineHeight: 1 }}>
                    ×
                  </Box>
                </IconButton>
              </Tooltip>
            </Stack>
          </Box>

          <Box
            data-testid="admin-tool-drawer-scroll-body"
            sx={{
              minHeight: 0,
              overflowY: 'auto',
              overflowX: 'hidden',
              overscrollBehavior: 'contain',
              WebkitOverflowScrolling: 'touch',
              px: { xs: 1, sm: 1.25 },
              py: 1.15,
            }}
          >
            {tools.map((tool) => (
              <Box
                key={tool.id}
                role="tabpanel"
                id={`admin-tool-panel-${tool.id}`}
                aria-label={tool.label}
                hidden={tool.id !== resolvedActiveToolId}
              >
                {tool.content}
              </Box>
            ))}
          </Box>
        </Box>
      </Drawer>

      <Menu
        id="admin-tool-menu"
        anchorEl={menuAnchor}
        open={menuAnchor !== null}
        onClose={() => setMenuAnchor(null)}
        MenuListProps={{ 'aria-label': t('adminTools.chooseTool') }}
      >
        {tools.map((tool) => (
          <MenuItem
            key={tool.id}
            selected={tool.id === resolvedActiveToolId}
            onClick={() => {
              setActiveToolId(tool.id)
              setMenuAnchor(null)
            }}
          >
            {tool.label}
          </MenuItem>
        ))}
      </Menu>
    </>
  )
}
