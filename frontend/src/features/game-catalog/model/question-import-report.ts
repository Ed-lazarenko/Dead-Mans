import type { ImportGameQuestionSkippedItem } from '../../../shared/api/contracts/index.ts'
import type { TFunction } from 'i18next'
import { downloadTextFile } from '../../../shared/lib/download-file.ts'

interface QuestionImportFailureReportInput {
  fileName: string
  importedCount: number
  skippedQuestions: ImportGameQuestionSkippedItem[]
  errorMessage?: string | null
}

function resolveSkippedQuestionReason(item: ImportGameQuestionSkippedItem, t: TFunction): string {
  switch (item.reasonCode) {
    case 'game_question.import_invalid_fields':
      return t('gameCatalog.questions.importReasons.invalidFields')
    case 'game_question.import_duplicate_code_in_file':
      return t('gameCatalog.questions.importReasons.duplicateCodeInFile')
    case 'game_question.import_category_unresolved':
      return t('gameCatalog.questions.importReasons.categoryUnresolved')
    case 'game_question.import_duplicate_code_existing':
      return t('gameCatalog.questions.importReasons.duplicateCodeExisting')
    default:
      if (
        item.reason ===
        'Missing or invalid required fields. Each question must include text, answer, and a non-negative reward.'
      ) {
        return t('gameCatalog.questions.importReasons.invalidFields')
      }

      if (
        item.reason.startsWith("External code '") &&
        item.reason.endsWith("' is duplicated inside the import file.")
      ) {
        return t('gameCatalog.questions.importReasons.duplicateCodeInFile')
      }

      if (item.reason === 'The selected category could not be resolved.') {
        return t('gameCatalog.questions.importReasons.categoryUnresolved')
      }

      if (item.reason.startsWith("External code '") && item.reason.endsWith("' already exists.")) {
        return t('gameCatalog.questions.importReasons.duplicateCodeExisting')
      }

      return item.reason
  }
}

export function formatSkippedQuestionWarning(
  item: ImportGameQuestionSkippedItem,
  t: TFunction,
): string {
  return `#${item.rowNumber}${item.questionText ? ` - ${item.questionText}` : ''}: ${resolveSkippedQuestionReason(item, t)}`
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
        reasonCode: item.reasonCode,
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
  downloadTextFile(content, `${baseFileName}-import-report-${timestamp}.json`, 'application/json')
}
