import { GameAdminToolsHost } from '../../features/admin-tools/GameAdminToolsHost.tsx'
import { GameModifiersPage } from '../../features/game-modifiers/GameModifiersPage.tsx'

export function GameModifiersRoutePage() {
  return (
    <>
      <GameModifiersPage />
      <GameAdminToolsHost />
    </>
  )
}
