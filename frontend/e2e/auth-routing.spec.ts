import { expect, test, type Page } from '@playwright/test'

interface MockSession {
  userId: string
  displayName: string
  roles: string[]
}

async function mockBackend(page: Page, session: MockSession | null) {
  await page.route(
    (url) => url.pathname === '/auth/me' || url.pathname.startsWith('/api/'),
    async (route) => {
      const requestUrl = new URL(route.request().url())

      if (requestUrl.pathname === '/auth/me') {
        if (session === null) {
          await route.fulfill({ status: 204 })
          return
        }

        await route.fulfill({ status: 200, json: session })
        return
      }

      await route.fulfill({ status: 204 })
    },
  )
}

test.beforeEach(async ({ page }) => {
  page.on('pageerror', (error) => {
    throw error
  })
  await page.addInitScript(() => window.localStorage.setItem('i18nextLng', 'en'))
})

test('redirects an anonymous visitor from the panel to sign in', async ({ page }) => {
  await mockBackend(page, null)

  await page.goto('/panel/game-board')

  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByRole('button', { name: /twitch/i })).toBeVisible()
})

test('redirects a viewer away from an admin-only route', async ({ page }) => {
  await mockBackend(page, {
    userId: 'c541269c-cb41-4e6c-9004-73d3f0b2ab93',
    displayName: 'Viewer',
    roles: ['viewer'],
  })

  await page.goto('/panel/game-setup')

  await expect(page).toHaveURL(/\/panel\/game-board$/)
  await expect(page.getByRole('heading', { name: /game board/i })).toBeVisible()
})

test('allows an administrator to open the question catalog', async ({ page }) => {
  await mockBackend(page, {
    userId: 'abf3680b-ac92-43ce-8c4f-c542f806e520',
    displayName: 'Administrator',
    roles: ['admin', 'viewer'],
  })

  await page.goto('/panel/catalog-questions')

  await expect(page).toHaveURL(/\/panel\/catalog-questions$/)
  await expect(page.getByRole('heading', { name: /question catalog/i })).toBeVisible()
})
