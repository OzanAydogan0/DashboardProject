import { useCallback, useEffect, useMemo, useState } from 'react'
import { RouterContext } from './context'
import { readBrowserLocation, resolveNavigation } from './navigation'

export function BrowserRouter({ children }) {
  const [location, setLocation] = useState(readBrowserLocation)

  useEffect(() => {
    const handlePopState = () => setLocation(readBrowserLocation())
    window.addEventListener('popstate', handlePopState)
    window.addEventListener('hashchange', handlePopState)
    return () => {
      window.removeEventListener('popstate', handlePopState)
      window.removeEventListener('hashchange', handlePopState)
    }
  }, [])

  const navigate = useCallback((to, options = {}) => {
    if (typeof to === 'number') {
      window.history.go(to)
      return
    }

    const { isExternal, url } = resolveNavigation(to)
    if (isExternal) {
      if (options.replace) {
        window.location.replace(url.href)
      } else {
        window.location.assign(url.href)
      }
      return
    }

    const nextPath = `${url.pathname}${url.search}${url.hash}`
    const nextState = Object.hasOwn(options, 'state') ? (options.state ?? null) : null

    if (options.replace) {
      window.history.replaceState(nextState, '', nextPath)
    } else {
      window.history.pushState(nextState, '', nextPath)
    }
    setLocation(readBrowserLocation())
  }, [])

  const value = useMemo(() => ({ location, navigate }), [location, navigate])

  return <RouterContext.Provider value={value}>{children}</RouterContext.Provider>
}
