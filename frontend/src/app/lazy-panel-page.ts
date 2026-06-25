import type { ComponentType } from 'react'
import { lazy } from 'react'

export function lazyPanelPage<TModule extends Record<string, ComponentType<unknown>>>(
  loader: () => Promise<TModule>,
  exportName: keyof TModule & string,
) {
  return lazy(async () => {
    const module = await loader()
    const component = module[exportName]

    if (!component) {
      const availableExports = Object.keys(module).filter((key) => key !== 'default')
      throw new Error(
        `Panel page export "${exportName}" is missing (available: ${availableExports.join(', ') || 'none'})`,
      )
    }

    return { default: component }
  })
}
