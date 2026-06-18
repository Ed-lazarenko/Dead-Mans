import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, FormControlLabel, Stack, Switch } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, ControlledFormTextField } from '../../../shared/ui/index.ts'
import type {
  CreateGameQuestionRequest,
  GameQuestionCatalogItem,
} from '../../../shared/api/contracts/index.ts'
import { createQuestionFormSchema, type QuestionFormValues } from '../model/question-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const questionFormId = 'catalog-question-form'

function toDefaultValues(initial: GameQuestionCatalogItem | undefined): QuestionFormValues {
  if (!initial) {
    return {
      vectorCode: '',
      category: '',
      text: '',
      answer: '',
      reward: '0',
      sortOrder: '0',
      isEnabled: true,
    }
  }

  return {
    vectorCode: initial.vectorCode,
    category: initial.category,
    text: initial.text,
    answer: initial.answer,
    reward: String(initial.reward),
    sortOrder: '0',
    isEnabled: initial.isEnabled,
  }
}

function toRequest(values: QuestionFormValues): CreateGameQuestionRequest {
  return {
    vectorCode: values.vectorCode.trim(),
    category: values.category.trim(),
    text: values.text.trim(),
    answer: values.answer.trim(),
    reward: Number.parseInt(values.reward, 10),
    isEnabled: values.isEnabled,
    sortOrder: Number.parseInt(values.sortOrder, 10),
  }
}

interface QuestionFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initial?: GameQuestionCatalogItem | undefined
  isBusy: boolean
  onClose: () => void
  onSubmit: (request: CreateGameQuestionRequest) => Promise<void>
}

function QuestionFormDialogBody({
  mode,
  initial,
  isBusy,
  onClose,
  onSubmit,
}: Omit<QuestionFormDialogProps, 'open'>) {
  const { t } = useTranslation()
  const schema = createQuestionFormSchema({
    required: t('gameCatalog.validation.required'),
    number: t('gameCatalog.validation.number'),
  })
  const { control, handleSubmit, setError, formState } = useForm<QuestionFormValues>({
    defaultValues: toDefaultValues(initial),
    resolver: zodResolver(schema),
  })

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(toRequest(values))
    } catch (error) {
      setError('root', { type: 'server', message: resolveCatalogErrorMessage(error, t) })
    }
  })

  return (
    <AppDialog
      open
      onClose={isBusy ? undefined : onClose}
      title={
        mode === 'create'
          ? t('gameCatalog.questions.createTitle')
          : t('gameCatalog.questions.editTitle')
      }
      actions={
        <>
          <AppButton tone="ghost" onClick={onClose} disabled={isBusy}>
            {t('gameCatalog.actions.cancel')}
          </AppButton>
          <AppButton type="submit" form={questionFormId} disabled={isBusy}>
            {t('gameCatalog.actions.save')}
          </AppButton>
        </>
      }
    >
      {formState.errors.root ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {formState.errors.root.message}
        </Alert>
      ) : null}
      <form id={questionFormId} onSubmit={(event) => void submit(event)}>
        <Stack spacing={1.5}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="vectorCode"
              label={t('gameCatalog.questions.fields.vectorCode')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="category"
              label={t('gameCatalog.questions.fields.category')}
              disabled={isBusy}
            />
          </Stack>
          <ControlledFormTextField
            control={control}
            name="text"
            label={t('gameCatalog.questions.fields.text')}
            multiline
            minRows={2}
            disabled={isBusy}
          />
          <ControlledFormTextField
            control={control}
            name="answer"
            label={t('gameCatalog.questions.fields.answer')}
            disabled={isBusy}
          />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="reward"
              type="number"
              label={t('gameCatalog.questions.fields.reward')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="sortOrder"
              type="number"
              label={t('gameCatalog.questions.fields.sortOrder')}
              disabled={isBusy}
            />
          </Stack>
          <Controller
            control={control}
            name="isEnabled"
            render={({ field }) => (
              <FormControlLabel
                control={
                  <Switch
                    checked={field.value}
                    onChange={(event) => field.onChange(event.target.checked)}
                    disabled={isBusy}
                  />
                }
                label={t('gameCatalog.questions.fields.isEnabled')}
              />
            )}
          />
        </Stack>
      </form>
    </AppDialog>
  )
}

export function QuestionFormDialog({ open, ...props }: QuestionFormDialogProps) {
  return open ? <QuestionFormDialogBody {...props} /> : null
}
