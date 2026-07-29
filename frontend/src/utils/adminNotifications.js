const STORAGE_KEY = 'password-change-requests'
const CHANGE_EVENT = 'password-change-request'

const dispatchChange = () => {
  window.dispatchEvent(new CustomEvent(CHANGE_EVENT))
}

export const getPasswordChangeRequests = () => {
  if (typeof window === 'undefined') return []

  try {
    const storedValue = window.localStorage.getItem(STORAGE_KEY)
    const parsedValue = storedValue ? JSON.parse(storedValue) : []
    return Array.isArray(parsedValue) ? parsedValue : []
  } catch {
    return []
  }
}

export const notifyPasswordChangeRequest = ({ fullName, email }) => {
  const request = {
    id: window.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`,
    fullName: fullName?.trim() || 'İsim soyisim girilmedi',
    email: email?.trim() || 'E-posta girilmedi',
    requestedAt: new Date().toISOString(),
    message: `${fullName?.trim() || 'İsim soyisim girilmedi'} adlı kullanıcı şifre değiştirme isteğinde bulundu.`,
  }

  const nextRequests = [request, ...getPasswordChangeRequests()].slice(0, 20)
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextRequests))
  window.dispatchEvent(new CustomEvent(CHANGE_EVENT, { detail: request }))

  return request
}

export const removePasswordChangeRequest = (requestId) => {
  if (typeof window === 'undefined') return

  const nextRequests = getPasswordChangeRequests()
    .filter(request => request.id !== requestId)

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextRequests))
  dispatchChange()
}

export const clearPasswordChangeRequests = () => {
  if (typeof window === 'undefined') return

  window.localStorage.removeItem(STORAGE_KEY)
  dispatchChange()
}
