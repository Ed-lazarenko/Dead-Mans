import { QueryClient } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import {
  acceptGameRegistrationInvitationMutationOptions,
  assignGameRegistrationPlayerToTeamMutationOptions,
  cancelPlayerGameRegistrationInvitationMutationOptions,
  confirmGameRegistrationTeamMutationOptions,
  createAdminGameRegistrationTeamMutationOptions,
  createPlayerGameRegistrationInvitationMutationOptions,
  createGameRegistrationTeamMutationOptions,
  declineGameRegistrationInvitationMutationOptions,
  joinGameRegistrationTeamMutationOptions,
  leaveGameRegistrationTeamMutationOptions,
  moveGameRegistrationTeamToSlotMutationOptions,
  rejectGameRegistrationTeamMutationOptions,
} from './game-registration-mutation-options.ts'
import {
  acceptGameRegistrationInvitation,
  assignGameRegistrationPlayerToTeam,
  cancelPlayerGameRegistrationInvitation,
  confirmGameRegistrationTeam,
  createAdminGameRegistrationTeam,
  createPlayerGameRegistrationInvitation,
  createGameRegistrationTeam,
  declineGameRegistrationInvitation,
  joinGameRegistrationTeam,
  leaveGameRegistrationTeam,
  moveGameRegistrationTeamToSlot,
  rejectGameRegistrationTeam,
} from './game-registration-api.ts'
import { gameRegistrationQueryKeys } from './game-registration-queries.ts'

describe('game registration mutation options', () => {
  it.each([
    [createGameRegistrationTeamMutationOptions, createGameRegistrationTeam],
    [joinGameRegistrationTeamMutationOptions, joinGameRegistrationTeam],
    [createAdminGameRegistrationTeamMutationOptions, createAdminGameRegistrationTeam],
    [leaveGameRegistrationTeamMutationOptions, leaveGameRegistrationTeam],
    [acceptGameRegistrationInvitationMutationOptions, acceptGameRegistrationInvitation],
    [declineGameRegistrationInvitationMutationOptions, declineGameRegistrationInvitation],
    [createPlayerGameRegistrationInvitationMutationOptions, createPlayerGameRegistrationInvitation],
    [cancelPlayerGameRegistrationInvitationMutationOptions, cancelPlayerGameRegistrationInvitation],
    [confirmGameRegistrationTeamMutationOptions, confirmGameRegistrationTeam],
    [rejectGameRegistrationTeamMutationOptions, rejectGameRegistrationTeam],
    [assignGameRegistrationPlayerToTeamMutationOptions, assignGameRegistrationPlayerToTeam],
    [moveGameRegistrationTeamToSlotMutationOptions, moveGameRegistrationTeamToSlot],
  ])('keeps each concrete mutation wired to its API operation', (factory, mutationFn) => {
    const options = factory(new QueryClient(), vi.fn())

    expect(options.mutationFn).toBe(mutationFn)
  })

  it('invalidates all registration queries after a successful mutation', async () => {
    const queryClient = new QueryClient()
    const invalidateQueries = vi
      .spyOn(queryClient, 'invalidateQueries')
      .mockResolvedValue(undefined)
    const options = createGameRegistrationTeamMutationOptions(queryClient, vi.fn())

    await options.onSuccess?.(undefined as never, true, undefined, undefined as never)

    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: gameRegistrationQueryKeys.all,
    })
  })

  it('uses the shared error handler for each concrete mutation', () => {
    const queryClient = new QueryClient()
    const onError = vi.fn()
    const options = rejectGameRegistrationTeamMutationOptions(queryClient, onError)
    const error = new Error('rejected')

    options.onError?.(error, 'team-1', undefined, undefined as never)

    expect(onError).toHaveBeenCalledWith(error)
  })
})
