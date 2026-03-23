import { Box, Dialog, DialogContent } from '@mui/material'
import type { LoadoutCell } from '../../../shared/api/contracts.ts'

interface LoadoutFullscreenDialogProps {
  cell: LoadoutCell | null
  onClose: () => void
}

export function LoadoutFullscreenDialog({
  cell,
  onClose,
}: LoadoutFullscreenDialogProps) {
  return (
    <Dialog open={Boolean(cell)} onClose={onClose} fullWidth maxWidth="lg">
      {cell && (
        <DialogContent
          sx={{
            bgcolor: 'transparent',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            p: 0,
          }}
        >
          <Box
            component="img"
            src={cell.imageUrl}
            alt={cell.label}
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              maxHeight: '90vh',
            }}
          />
        </DialogContent>
      )}
    </Dialog>
  )
}
