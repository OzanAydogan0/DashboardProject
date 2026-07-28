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
