import type { ImportGameQuestionSkippedItem } from '../../../shared/api/contracts/index.ts'

export interface QuestionImportFailureReportInput {
  fileName: string
  importedCount: number
  skippedQuestions: ImportGameQuestionSkippedItem[]
  errorMessage?: string | null
}

export function formatSkippedQuestionWarning(item: ImportGameQuestionSkippedItem): string {
  return `#${item.rowNumber}${item.questionText ? ` - ${item.questionText}` : ''}: ${item.reason}`
}

export function buildQuestionImportFailureReport({
  fileName,
  importedCount,
  skippedQuestions,
  errorMessage,
}: QuestionImportFailureReportInput): string {
  return JSON.stringify(
    {
      generatedAt: new Date().toISOString(),
      sourceFileName: fileName,
      importedCount,
      skippedCount: skippedQuestions.length,
      errorMessage: errorMessage ?? null,
      skippedQuestions: skippedQuestions.map((item) => ({
        rowNumber: item.rowNumber,
        questionText: item.questionText ?? null,
        reason: item.reason,
        sourceQuestion: item.sourceQuestion ?? null,
      })),
    },
    null,
    2,
  )
}

export function downloadQuestionImportFailureReport(
  report: QuestionImportFailureReportInput,
): void {
  const content = buildQuestionImportFailureReport(report)
  const baseFileName = report.fileName.replace(/\.[^.]+$/, '') || 'question-import'
  const timestamp = new Date().toISOString().replaceAll(':', '-')
  const blob = new Blob([content], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `${baseFileName}-import-report-${timestamp}.json`
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}
