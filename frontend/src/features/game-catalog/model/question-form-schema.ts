import { z } from 'zod'

interface QuestionFormSchemaMessages {
  required: string
  number: string
}

export function createQuestionFormSchema(messages: QuestionFormSchemaMessages) {
  return z.object({
    category: z.string().trim().min(1, messages.required).max(64, messages.required),
    text: z.string().trim().min(1, messages.required).max(2000, messages.required),
    answer: z.string().trim().min(1, messages.required).max(500, messages.required),
    reward: z.string().regex(/^\d+$/, messages.number),
    sortOrder: z.string().regex(/^\d+$/, messages.number),
    isEnabled: z.boolean(),
  })
}

export type QuestionFormValues = z.infer<ReturnType<typeof createQuestionFormSchema>>
