import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { gameQuestionCatalogQueryOptions } from '../game-questions/index.ts'

/**
 * Read-only catalog access for the per-game enabled-question screen. It loads
 * the global question catalog and exposes client-side search/category filtering;
 * membership in the current game is owned by the setup draft, not by this hook.
 */
export function useGameSetupQuestionsCatalog() {
  const [search, setSearch] = useState('')
  const [activeCategory, setActiveCategory] = useState<string | null>(null)

  const catalogQuery = useQuery(gameQuestionCatalogQueryOptions({ search }))

  const questions = useMemo(() => catalogQuery.data ?? [], [catalogQuery.data])

  const categories = useMemo(() => {
    return Array.from(new Set(questions.map((question) => question.categoryName))).sort((a, b) =>
      a.localeCompare(b),
    )
  }, [questions])

  const filteredQuestions = useMemo(() => {
    if (!activeCategory) {
      return questions
    }

    return questions.filter((question) => question.categoryName === activeCategory)
  }, [activeCategory, questions])

  return {
    search,
    setSearch,
    activeCategory,
    setActiveCategory,
    catalogQuery,
    categories,
    filteredQuestions,
  }
}
