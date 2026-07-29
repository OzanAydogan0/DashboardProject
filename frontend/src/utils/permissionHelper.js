export const parseStoredUser = () => {
  const userString = localStorage.getItem('user')
  if (!userString || userString === 'undefined') return null

  try {
    return JSON.parse(userString)
  } catch {
    return null
  }
}

export const getUserRole = () => {
  const user = parseStoredUser()
  return (
    user?.userRole ||
    user?.UserRole ||
    user?.role ||
    user?.Role ||
    ''
  ).toString().trim()
}

export const getUserId = () => {
  const user = parseStoredUser()
  return user?.userId || user?.UserId || user?.id || user?.Id || ''
}

export const normalizeRole = (role) => {
  if (!role) return ''
  const trimmed = role.toString().trim()
  if (trimmed === 'Üst Yönetim' || trimmed === 'Üst Yönetim İzleyicisi') return 'Üst Yönetim İzleyicisi'
  return trimmed
}

export const getUserRecordId = (user) =>
  user?.userId || user?.UserId || user?.id || user?.Id || ''

export const getUserRecordRole = (user) =>
  normalizeRole(
    user?.userRole ||
    user?.UserRole ||
    user?.role ||
    user?.Role ||
    '',
  )

export const isAssignableProjectUser = (user) =>
  !['Sistem Yöneticisi', 'Üst Yönetim İzleyicisi'].includes(getUserRecordRole(user))

export const getAssignableProjectUsers = (users) =>
  (Array.isArray(users) ? users : []).filter(isAssignableProjectUser)

export const getDefaultProjectAssigneeId = (users) => {
  const assignableUsers = getAssignableProjectUsers(users)
  const currentUserId = getUserId()
  const currentUser = assignableUsers.find(user => getUserRecordId(user) === currentUserId)
  return getUserRecordId(currentUser || assignableUsers[0])
}

export const getValidProjectAssigneeId = (users, userId) =>
  getAssignableProjectUsers(users).some(user => getUserRecordId(user) === userId)
    ? userId
    : ''

export const isSystemAdmin = () => normalizeRole(getUserRole()) === 'Sistem Yöneticisi'
export const isExecutive = () => ['Üst Yönetim İzleyicisi'].includes(normalizeRole(getUserRole()))

export const canCreateProject = () => isSystemAdmin()

export const canEditProject = (project) => {
  if (isSystemAdmin()) return true

  const managerId =
    project?.projectManagerUserId ||
    project?.ProjectManagerUserId ||
    project?.projectManagerId ||
    project?.ProjectManagerId ||
    ''

  return managerId && managerId === getUserId()
}

export const canWriteProject = () => !isExecutive()
