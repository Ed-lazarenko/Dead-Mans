import { z } from 'zod'

interface ModifierFormSchemaMessages {
  required: string
  code: string
  number: string
  limit: string
}

export function createModifierFormSchema(
  messages: ModifierFormSchemaMessages,
  validateCode: boolean,
) {
  return z.object({
    code: validateCode
      ? z
          .string()
          .trim()
          .min(1, messages.code)
          .max(64, messages.code)
          .regex(/^[a-z0-9_]+$/, messages.code)
      : z.string(),
    name: z.string().trim().min(1, messages.required).max(128, messages.required),
    description: z.string().trim().min(1, messages.required).max(2000, messages.required),
    kind: z.enum(['active', 'passive']),
    category: z.string().trim().min(1, messages.required).max(64, messages.required),
    scoringType: z.string().trim().min(1, messages.required).max(64, messages.required),
    tier: z.enum(['low', 'mid', 'high']),
    activationCost: z.string().regex(/^\d+$/, messages.number),
    defaultLimitPerGame: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
    iconEmoji: z.string().max(16),
    activationCommand: z.string().max(128),
  })
}

export type ModifierFormValues = z.infer<ReturnType<typeof createModifierFormSchema>>
