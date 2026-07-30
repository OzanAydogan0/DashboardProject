import assert from 'node:assert/strict'
import test from 'node:test'
import { createHref, resolveNavigation } from './navigation.js'

const currentLocation = {
  hash: '#current',
  href: 'https://dashboard.example/projects/42?tab=risks#current',
  origin: 'https://dashboard.example',
  pathname: '/projects/42',
  search: '?tab=risks',
}

test('object destinations produce browser-compatible hrefs', () => {
  assert.equal(
    createHref({ pathname: '/projects/7', search: 'tab=evm' }, currentLocation),
    '/projects/7?tab=evm'
  )
})

test('same-origin and external destinations are distinguished', () => {
  assert.equal(resolveNavigation('/reports', currentLocation).isExternal, false)
  assert.equal(
    resolveNavigation('//outside.example/report', currentLocation).isExternal,
    true
  )
})
