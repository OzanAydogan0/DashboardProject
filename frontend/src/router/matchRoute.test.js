import assert from 'node:assert/strict'
import test from 'node:test'
import { matchRoute } from './matchRoute.js'

test('root and static routes match exactly', () => {
  assert.deepEqual(matchRoute('/', '/'), { params: {} })
  assert.deepEqual(matchRoute('/projects', '/projects/'), { params: {} })
  assert.deepEqual(matchRoute('/projects', '/PROJECTS'), { params: {} })
  assert.equal(matchRoute('/projects', '/projects/42'), null)
})

test('dynamic route parameters are decoded', () => {
  assert.deepEqual(
    matchRoute('/projects/:id', '/projects/PRJ%2001'),
    { params: { id: 'PRJ 01' } }
  )
  assert.equal(matchRoute('/projects/:id', '/projects'), null)
})

test('wildcard routes capture the remaining path', () => {
  assert.deepEqual(
    matchRoute('/files/*', '/files/reports/2026'),
    { params: { '*': 'reports/2026' } }
  )
  assert.deepEqual(
    matchRoute('*', '/unknown/deep-link'),
    { params: { '*': 'unknown/deep-link' } }
  )
})
