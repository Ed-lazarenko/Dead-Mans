import { PageShell } from '../../shared/ui/index.ts'
import { GameSetupQuestionsSection } from './ui/GameSetupQuestionsSection.tsx'

export function AdminGameQuestionsPage() {
  return (
    <PageShell>
      <GameSetupQuestionsSection />
    </PageShell>
  )
}
