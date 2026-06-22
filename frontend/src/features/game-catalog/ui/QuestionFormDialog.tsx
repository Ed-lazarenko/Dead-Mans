import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, FormControlLabel, Stack, Switch } from '@mui/material'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import {
  AppButton,
  AppDialog,
  ControlledFormTextField,
  FormSelect,
} from '../../../shared/ui/index.ts'
import type {
  CreateGameQuestionRequest,
  GameQuestionCategoryItem,
  GameQuestionCatalogItem,
} from '../../../shared/api/contracts/index.ts'
import { createQuestionFormSchema, type QuestionFormValues } from '../model/question-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const questionFormId = 'catalog-question-form'

function toDefaultValues(
  initial: GameQuestionCatalogItem | undefined,
  categories: readonly GameQuestionCategoryItem[],
): QuestionFormValues {
  if (!initial) {
    return {
      category: categories[0]?.name ?? '',
      text: '',
      answer: '',
      reward: '0',
      sortOrder: '0',
      isEnabled: true,
    }
  }

  return {
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
  categories: readonly GameQuestionCategoryItem[]
  isBusy: boolean
  onClose: () => void
  onSubmit: (request: CreateGameQuestionRequest) => Promise<void>
}

function QuestionFormDialogBody({
  mode,
  initial,
  categories,
  isBusy,
  onClose,
  onSubmit,
}: Omit<QuestionFormDialogProps, 'open'>) {
  const { t } = useTranslation()
  const schema = createQuestionFormSchema({
    required: t('gameCatalog.validation.required'),
    number: t('gameCatalog.validation.number'),
  })
  const { control, handleSubmit, setError, setValue, watch, formState } = useForm<QuestionFormValues>(
    {
      defaultValues: toDefaultValues(initial, categories),
      resolver: zodResolver(schema),
    },
  )
  const categoryValue = watch('category')

  useEffect(() => {
    const firstCategory = categories[0]
    if (categoryValue.length === 0 && firstCategory) {
      setValue('category', firstCategory.name)
    }
  }, [categories, categoryValue, setValue])

  const categoryOptions = categories.map((category) => ({
    value: category.name,
    label: category.name,
  }))

  const hasCategories = categoryOptions.length > 0

  const submit = handleSubmit(async (values) => {
    if (!hasCategories) {
      setError('category', {
        type: 'manual',
        message: t('gameCatalog.questions.noCategories'),
      })
      return
    }

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
          <AppButton type="submit" form={questionFormId} disabled={isBusy || !hasCategories}>
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
      {!hasCategories ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {t('gameCatalog.questions.noCategories')}
        </Alert>
      ) : null}
      <form id={questionFormId} onSubmit={(event) => void submit(event)}>
        <Stack spacing={1.5}>
          <Controller
            control={control}
            name="category"
            render={({ field, fieldState }) => (
              <FormSelect
                value={field.value}
                options={categoryOptions}
                label={t('gameCatalog.questions.fields.category')}
                disabled={isBusy || !hasCategories}
                error={fieldState.invalid}
                helperText={fieldState.error?.message}
                onChange={field.onChange}
              />
            )}
          />
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
