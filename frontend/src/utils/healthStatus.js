export const HEALTH_STATUS = Object.freeze({
  CRITICAL: 'Kritik',
  MEDIUM: 'Orta',
  GOOD: 'İyi',
  UNCERTAIN: 'Belirsiz',
})

export const HEALTH_STATUS_OPTIONS = [
  HEALTH_STATUS.CRITICAL,
  HEALTH_STATUS.MEDIUM,
  HEALTH_STATUS.GOOD,
  HEALTH_STATUS.UNCERTAIN,
]

export const normalizeHealthStatus = (value) => {
  if (!value) return HEALTH_STATUS.UNCERTAIN

  const normalizedValue = String(value).trim().toLocaleLowerCase('tr-TR')

  if (['kırmızı', 'kirmizi', 'kritik', 'red'].includes(normalizedValue)) {
    return HEALTH_STATUS.CRITICAL
  }

  if (['sarı', 'sari', 'orta', 'yellow'].includes(normalizedValue)) {
    return HEALTH_STATUS.MEDIUM
  }

  if (['yeşil', 'yesil', 'iyi', 'düşük', 'dusuk', 'green'].includes(normalizedValue)) {
    return HEALTH_STATUS.GOOD
  }

  return HEALTH_STATUS.UNCERTAIN
}
