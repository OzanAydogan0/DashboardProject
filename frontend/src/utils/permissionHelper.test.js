import assert from 'node:assert/strict'
import test from 'node:test'
import {
  getAssignableProjectUsers,
  isAssignableProjectUser,
} from './permissionHelper.js'

test('active administrators and project managers can own a project', () => {
  assert.equal(isAssignableProjectUser({
    userRole: 'Sistem Yöneticisi',
    userStatus: 'Aktif',
  }), true)
  assert.equal(isAssignableProjectUser({
    userRole: 'Proje Yöneticisi',
    userStatus: 'Aktif',
  }), true)
})

test('executive and inactive users cannot own a project', () => {
  const users = [
    { userId: 'USR-1', userRole: 'Üst Yönetim İzleyicisi', userStatus: 'Aktif' },
    { userId: 'USR-2', userRole: 'Proje Yöneticisi', userStatus: 'Pasif' },
    { userId: 'USR-3', userRole: 'Sistem Yöneticisi', userStatus: 'Aktif' },
  ]

  assert.deepEqual(
    getAssignableProjectUsers(users).map((user) => user.userId),
    ['USR-3'],
  )
})
