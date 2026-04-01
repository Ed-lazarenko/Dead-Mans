export const queryKeys = {
  gameBoard: {
    all: ['gameBoard'] as const,
    currentSnapshot: () => ['gameBoard', 'currentSnapshot'] as const,
  },
}
