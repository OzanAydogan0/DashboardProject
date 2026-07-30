const trimSlashes = (value) => value.replace(/^\/+|\/+$/g, '')

const getSegments = (value) => {
  const trimmed = trimSlashes(value)
  return trimmed ? trimmed.split('/') : []
}

const safeDecode = (value) => {
  try {
    return decodeURIComponent(value)
  } catch {
    return value
  }
}

export function matchRoute(pattern, pathname) {
  const patternSegments = getSegments(pattern)
  const pathSegments = getSegments(pathname)
  const params = {}

  for (let index = 0; index < patternSegments.length; index += 1) {
    const patternSegment = patternSegments[index]
    const pathSegment = pathSegments[index]

    if (patternSegment === '*') {
      params['*'] = safeDecode(pathSegments.slice(index).join('/'))
      return { params }
    }

    if (pathSegment === undefined) {
      return null
    }

    if (patternSegment.startsWith(':')) {
      const paramName = patternSegment.slice(1)
      if (!paramName) return null
      params[paramName] = safeDecode(pathSegment)
      continue
    }

    if (safeDecode(patternSegment).toLocaleLowerCase() !== safeDecode(pathSegment).toLocaleLowerCase()) {
      return null
    }
  }

  return patternSegments.length === pathSegments.length ? { params } : null
}
