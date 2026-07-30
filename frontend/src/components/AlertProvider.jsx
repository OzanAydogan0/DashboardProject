import { useCallback, useEffect, useMemo, useState } from 'react'
import { AlertContext } from './alertContext'
import './AlertProvider.css'

export function AlertProvider({ children }) {
  const [alerts, setAlerts] = useState([])

  const removeAlert = useCallback((id) => {
    setAlerts((prev) => prev.filter((alert) => alert.id !== id))
  }, [])

  const addAlert = useCallback((message, type = 'info', duration = 3500) => {
    const id = window.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`
    setAlerts((prev) => [...prev, { id, message, type }])

    window.setTimeout(() => {
      removeAlert(id)
    }, duration)
  }, [removeAlert])

  useEffect(() => {
    const handleAlert = (event) => {
      const { message, type, duration } = event.detail || {}
      if (!message) return
      addAlert(message, type, duration)
    }

    window.addEventListener('app:alert', handleAlert)
    return () => window.removeEventListener('app:alert', handleAlert)
  }, [addAlert])

  const value = useMemo(() => ({ addAlert, removeAlert }), [addAlert, removeAlert])

  return (
    <AlertContext.Provider value={value}>
      {children}
      <div className="global-alert-stack" role="status" aria-live="polite">
        {alerts.map((alert) => (
          <div key={alert.id} className={`global-alert-card ${alert.type || 'info'}`}>
            <div className="global-alert-icon">
              {alert.type === 'success' ? '✓' : alert.type === 'error' ? '!' : 'i'}
            </div>
            <div className="global-alert-content">
              <strong>{alert.type === 'success' ? 'Başarılı' : alert.type === 'error' ? 'Hata' : 'Bilgi'}</strong>
              <p>{alert.message}</p>
            </div>
          </div>
        ))}
      </div>
    </AlertContext.Provider>
  )
}
