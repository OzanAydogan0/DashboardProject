export function createHref(to, currentLocation = window.location) {
  if (typeof to === 'string') {
    return to || `${currentLocation.pathname}${currentLocation.search}${currentLocation.hash}`
  }

  const pathname = to?.pathname ?? currentLocation.pathname
  const search = to?.search
    ? (String(to.search).startsWith('?') ? String(to.search) : `?${to.search}`)
    : ''
  const hash = to?.hash
    ? (String(to.hash).startsWith('#') ? String(to.hash) : `#${to.hash}`)
    : ''

  return `${pathname}${search}${hash}`
}

export function resolveNavigation(to, currentLocation = window.location) {
  const href = createHref(to, currentLocation)
  const url = new URL(href, currentLocation.href)

  return {
    href,
    isExternal: url.origin !== currentLocation.origin,
    url,
  }
}

export function readBrowserLocation() {
  return {
    pathname: window.location.pathname,
    search: window.location.search,
    hash: window.location.hash,
    state: window.history.state,
  }
}
