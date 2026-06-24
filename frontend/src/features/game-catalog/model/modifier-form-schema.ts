import { z } from 'zod'

export const modifierMechanicTypes = [
  'rule_only',
  'restriction_with_reward',
  'kill_counter',
  'multiplier',
  'mentor',
] as const

export const modifierActivationLimitScopes = ['game'] as const

interface ModifierFormSchemaMessages {
  required: string
  number: string
  limit: string
}

export function createModifierFormSchema(messages: ModifierFormSchemaMessages) {
  return z
    .object({
      name: z.string().trim().min(1, messages.required).max(128, messages.required),
      description: z.string().trim().min(1, messages.required).max(2000, messages.required),
      kind: z.enum(['active', 'passive']),
      mechanicType: z.enum(modifierMechanicTypes),
      tier: z.enum(['low', 'mid', 'high']),
      activationCost: z.string().regex(/^\d+$/, messages.number),
      activationLimitCount: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      activationLimitScope: z.enum(modifierActivationLimitScopes),
      conflictingModifierIds: z.array(z.string()),
      iconEmoji: z.string().max(16),
      activationCommand: z.string().max(128),
      durationSeconds: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      ruleText: z.string().max(512),
      perKillBonus: z.string().regex(/^(-?\d+)?$/, messages.number),
      failurePenaltyPoints: z.string().regex(/^(-?\d+)?$/, messages.number),
      killDeltaMode: z.string().max(64),
      killDeltaValue: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      killCondition: z.string().max(128),
      excludedWeapons: z.string().max(512),
      multiplierTarget: z.string().max(64),
      multiplierDelta: z.string().regex(/^(-?\d+([.,]\d+)?)?$/, messages.number),
      activeWindow: z.string().max(64),
      stopCondition: z.string().max(128),
      mentorLoadoutText: z.string().max(512),
      mentorDurationSeconds: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      mentorCanBeRevived: z.enum(['true', 'false']),
      mentorCanBeKilled: z.enum(['true', 'false']),
      mentorKillsCreditToTeam: z.enum(['true', 'false']),
    })
    .superRefine((values, ctx) => {
      if (
        values.mechanicType === 'restriction_with_reward' &&
        values.perKillBonus.trim() === '' &&
        values.failurePenaltyPoints.trim() === ''
      ) {
        ctx.addIssue({
          code: 'custom',
          path: ['perKillBonus'],
          message: messages.required,
        })
      }

      if (values.mechanicType === 'kill_counter' && values.killDeltaValue.trim() === '') {
        ctx.addIssue({
          code: 'custom',
          path: ['killDeltaValue'],
          message: messages.required,
        })
      }

      if (values.mechanicType === 'multiplier' && values.multiplierDelta.trim() === '') {
        ctx.addIssue({
          code: 'custom',
          path: ['multiplierDelta'],
          message: messages.required,
        })
      }

      if (values.mechanicType === 'mentor' && values.mentorLoadoutText.trim() === '') {
        ctx.addIssue({
          code: 'custom',
          path: ['mentorLoadoutText'],
          message: messages.required,
        })
      }
    })
}

export type ModifierFormValues = z.infer<ReturnType<typeof createModifierFormSchema>>
export type ModifierMechanicType = (typeof modifierMechanicTypes)[number]
