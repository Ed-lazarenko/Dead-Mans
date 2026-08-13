import { describe, expect, it } from 'vitest'
import { huntPalette } from '../../shared/theme/hunt-palette.ts'
import { appComponentOverrides } from './component-overrides.ts'

describe('appComponentOverrides', () => {
  it('keeps disabled button labels readable on every application surface', () => {
    const rootStyles = appComponentOverrides.MuiButton?.styleOverrides?.root

    expect(rootStyles).toMatchObject({
      '&.Mui-disabled': {
        color: huntPalette.parchment,
        backgroundImage: 'none',
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
      const disabledBackground = blendHex(huntPalette.soot, surface, 0.58)
      expect(
        contrastRatio(hexToRgb(huntPalette.parchment), disabledBackground),
      ).toBeGreaterThanOrEqual(4.5)
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
  const foregroundRgb = hexToRgb(foreground)
  const backgroundRgb = hexToRgb(background)

  return foregroundRgb.map(
    (channel, index) => channel * alpha + backgroundRgb[index]! * (1 - alpha),
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
