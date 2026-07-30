import { useCallback, useContext, useMemo } from 'react'
import { ParamsContext, RouterContext } from './context'

const useRouter = () => {
  const router = useContext(RouterContext)
  if (!router) {
    throw new Error('Router hooks must be used within BrowserRouter')
  }
  return router
}

export function useLocation() {
  return useRouter().location
}

export function useNavigate() {
  return useRouter().navigate
}

export function useParams() {
  return useContext(ParamsContext)
}

export function useSearchParams(defaultInit) {
  const { location, navigate } = useRouter()

  const searchParams = useMemo(() => {
    const params = new URLSearchParams(location.search)
    if (!defaultInit) return params

    const defaults = new URLSearchParams(defaultInit)
    defaults.forEach((value, key) => {
      if (!params.has(key)) params.append(key, value)
    })
    return params
  }, [defaultInit, location.search])

  const setSearchParams = useCallback((nextInit, options = {}) => {
    const nextValue = typeof nextInit === 'function'
      ? nextInit(new URLSearchParams(searchParams))
      : nextInit
    const nextParams = new URLSearchParams(nextValue)
    const nextSearch = nextParams.toString()

    navigate(
      `${location.pathname}${nextSearch ? `?${nextSearch}` : ''}${location.hash}`,
      options
    )
  }, [location.hash, location.pathname, navigate, searchParams])

  return [searchParams, setSearchParams]
}
