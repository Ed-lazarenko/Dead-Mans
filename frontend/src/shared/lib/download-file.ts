export function downloadBlobFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export function downloadTextFile(
  content: string,
  fileName: string,
  mimeType = 'text/plain; charset=utf-8',
): void {
  downloadBlobFile(new Blob([content], { type: mimeType }), fileName)
}
