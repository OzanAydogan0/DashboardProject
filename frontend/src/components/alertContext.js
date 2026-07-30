import { createContext, useContext } from 'react'

export const AlertContext = createContext(null)

export const showAppAlert = (message, type = 'info', duration = 3500) => {
  window.dispatchEvent(
    new CustomEvent('app:alert', {
      detail: { message, type, duration },
    })
  )
}

export function useAlert() {
  const context = useContext(AlertContext)
  if (!context) {
    throw new Error('useAlert must be used within AlertProvider')
  }
  return context
}
