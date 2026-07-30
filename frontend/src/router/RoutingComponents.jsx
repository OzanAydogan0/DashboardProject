import { Children, isValidElement, useContext, useEffect } from 'react'
import { OutletContext, ParamsContext } from './context'
import { useLocation, useNavigate } from './hooks'
import { createHref, resolveNavigation } from './navigation'
import { matchRoute } from './matchRoute'

const getRouteElements = (children) => (
  Children.toArray(children).filter((child) => isValidElement(child))
)

const withOutlet = (element, outlet) => {
  if (!element) return outlet
  return (
    <OutletContext.Provider value={outlet}>
      {element}
    </OutletContext.Provider>
  )
}

const findMatchingBranch = (children, pathname) => {
  for (const route of getRouteElements(children)) {
    const { path, element, children: nestedChildren } = route.props

    if (path) {
      const match = matchRoute(path, pathname)
      if (!match) continue

      const nestedMatch = nestedChildren
        ? findMatchingBranch(nestedChildren, pathname)
        : null

      return {
        element: nestedMatch ? withOutlet(element, nestedMatch.element) : element,
        params: { ...match.params, ...(nestedMatch?.params || {}) },
      }
    }

    const nestedMatch = findMatchingBranch(nestedChildren, pathname)
    if (nestedMatch) {
      return {
        element: withOutlet(element, nestedMatch.element),
        params: nestedMatch.params,
      }
    }
  }

  return null
}

export function Routes({ children }) {
  const location = useLocation()
  const branch = findMatchingBranch(children, location.pathname)

  if (!branch) return null

  return (
    <ParamsContext.Provider value={branch.params}>
      {branch.element}
    </ParamsContext.Provider>
  )
}

export function Route() {
  return null
}

export function Outlet() {
  return useContext(OutletContext)
}

export function Navigate({ to, replace = false, state }) {
  const navigate = useNavigate()

  useEffect(() => {
    navigate(to, { replace, state })
  }, [navigate, replace, state, to])

  return null
}

export function Link({
  children,
  download,
  onClick,
  replace = false,
  state,
  target,
  to,
  ...anchorProps
}) {
  const navigate = useNavigate()
  const href = createHref(to)

  const handleClick = (event) => {
    onClick?.(event)
    if (
      event.defaultPrevented ||
      event.button !== 0 ||
      event.metaKey ||
      event.altKey ||
      event.ctrlKey ||
      event.shiftKey ||
      (download !== undefined && download !== false) ||
      (target && target !== '_self') ||
      resolveNavigation(to).isExternal
    ) {
      return
    }

    event.preventDefault()
    navigate(to, { replace, state })
  }

  return (
    <a
      {...anchorProps}
      download={download}
      href={href}
      onClick={handleClick}
      target={target}
    >
      {children}
    </a>
  )
}
