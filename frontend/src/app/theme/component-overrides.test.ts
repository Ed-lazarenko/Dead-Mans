import { describe, expect, it } from 'vitest'
import { alpha } from '@mui/material/styles'
import { huntPalette } from '../../shared/theme/hunt-palette.ts'
import { appComponentOverrides } from './component-overrides.ts'

describe('appComponentOverrides', () => {
  it('keeps disabled buttons subdued without making their labels illegible', () => {
    const rootStyles = appComponentOverrides.MuiButton?.styleOverrides?.root

    expect(rootStyles).toMatchObject({
      '&.Mui-disabled': {
        color: alpha(huntPalette.parchmentMuted, 0.82),
        borderColor: alpha(huntPalette.parchmentMuted, 0.16),
        backgroundColor: alpha(huntPalette.soot, 0.18),
        backgroundImage: 'none',
        boxShadow: 'none',
        opacity: 1,
      },
    })

    for (const surface of [
      huntPalette.soot,
      huntPalette.charcoal,
      huntPalette.bark,
      huntPalette.leather,
      huntPalette.moss,
      huntPalette.mossDeep,
      huntPalette.murk,
    ]) {
      const disabledBackground = blendHex(huntPalette.soot, surface, 0.18)
      const disabledLabel = blendRgb(hexToRgb(huntPalette.parchmentMuted), disabledBackground, 0.82)
      expect(contrastRatio(disabledLabel, disabledBackground)).toBeGreaterThanOrEqual(3)
    }
  })
})

type Rgb = readonly [number, number, number]

function hexToRgb(value: string): Rgb {
  return [1, 3, 5].map(
    (offset) => Number.parseInt(value.slice(offset, offset + 2), 16) / 255,
  ) as unknown as Rgb
}

function blendHex(foreground: string, background: string, alpha: number): Rgb {
  return blendRgb(hexToRgb(foreground), hexToRgb(background), alpha)
}

function blendRgb(foreground: Rgb, background: Rgb, alpha: number): Rgb {
  return foreground.map(
    (channel, index) => channel * alpha + background[index]! * (1 - alpha),
  ) as unknown as Rgb
}

function contrastRatio(left: Rgb, right: Rgb) {
  const leftLuminance = relativeLuminance(left)
  const rightLuminance = relativeLuminance(right)

  return (
    (Math.max(leftLuminance, rightLuminance) + 0.05) /
    (Math.min(leftLuminance, rightLuminance) + 0.05)
  )
}

function relativeLuminance(rgb: Rgb) {
  const [red, green, blue] = rgb.map((channel) =>
    channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4,
  )

  return 0.2126 * red! + 0.7152 * green! + 0.0722 * blue!
}
