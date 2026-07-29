import { useEffect, useEffectEvent, useState } from 'react'
import './SettingsPage.css'
import { projectService } from '../services/projectService'
import { useAlert } from '../components/AlertProvider'
import {
  clearPasswordChangeRequests,
  getPasswordChangeRequests,
  removePasswordChangeRequest,
} from '../utils/adminNotifications'

const roleOptions = [
  'Sistem Yöneticisi',
  'Proje Yöneticisi',
  'Üst Yönetim İzleyicisi',
]

const userStatusOptions = ['Aktif', 'Pasif']
const customerTypeOptions = ['Kurumsal', 'Hükümet', 'Sivil Toplum', 'Diğer']
const customerStatusOptions = ['Aktif', 'Pasif']

const tabs = [
  { id: 'notifications', label: 'Bildirimler' },
  { id: 'users', label: 'Kullanıcılar ve Yetkiler' },
  { id: 'customers', label: 'Müşteriler' },
  { id: 'logs', label: 'Loglar' },
]

const entityLabels = {
  actions: 'Aksiyon',
  customers: 'Müşteri',
  evm_records: 'EVM Kaydı',
  issues: 'Sorun',
  management_decisions: 'Yönetim Kararı',
  milestones: 'Kilometre Taşı',
  pir_reports: 'PIR Raporu',
  programs: 'Program',
  project_users: 'Proje Ataması',
  projects: 'Proje',
  risks: 'Risk',
  users: 'Kullanıcı',
}

const actionLabels = {
  INSERT: 'Oluşturma',
  UPDATE: 'Güncelleme',
  DELETE: 'Silme',
}

const formatAuditValues = (value) => {
  if (!value) return 'Değer yok'

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

const getInitials = (fullName = '') => fullName
  .split(' ')
  .filter(Boolean)
  .slice(0, 2)
  .map(part => part[0])
  .join('')
  .toLocaleUpperCase('tr-TR')

const getIsAdmin = () => {
  if (typeof window === 'undefined') return false

  const userString = window.localStorage.getItem('user')
  if (!userString) return false

  try {
    const storedUser = JSON.parse(userString)
    const role = storedUser?.userRole || storedUser?.UserRole || storedUser?.role || storedUser?.Role
    return role === 'Sistem Yöneticisi'
  } catch {
    return false
  }
}

function SettingsPage() {
  const { addAlert } = useAlert()
  const [isAdmin] = useState(getIsAdmin)
  const [activeTab, setActiveTab] = useState('notifications')
  const [loading, setLoading] = useState(false)
  const [logsLoading, setLogsLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [messageType, setMessageType] = useState('success')
  const [customers, setCustomers] = useState([])
  const [users, setUsers] = useState([])
  const [projects, setProjects] = useState([])
  const [auditLogs, setAuditLogs] = useState([])
  const [logSearch, setLogSearch] = useState('')
  const [logActionFilter, setLogActionFilter] = useState('')
  const [newCustomer, setNewCustomer] = useState({
    CustomerName: '',
    CustomerType: customerTypeOptions[0],
    CustomerStatus: customerStatusOptions[0],
  })
  const [editingCustomerId, setEditingCustomerId] = useState(null)
  const [newUser, setNewUser] = useState({
    Email: '',
    FullName: '',
    Role: roleOptions[1],
    Password: '',
  })
  const [userEdits, setUserEdits] = useState({})
  const [passwordChangeRequests, setPasswordChangeRequests] = useState(getPasswordChangeRequests)

  useEffect(() => {
    const refreshRequests = () => {
      setPasswordChangeRequests(getPasswordChangeRequests())
    }

    window.addEventListener('password-change-request', refreshRequests)
    return () => window.removeEventListener('password-change-request', refreshRequests)
  }, [])

  const showSuccess = (successMessage) => {
    setMessage(successMessage)
    setMessageType('success')
    setError('')
    addAlert(successMessage, 'success')
  }

  const showError = (requestError, fallbackMessage) => {
    const errorMessage = requestError?.response?.data?.message || fallbackMessage
    setError(errorMessage)
    setMessage('')
    addAlert(errorMessage, 'error')
  }

  const loadAdminData = async () => {
    setLoading(true)
    setError('')

    try {
      const [customerData, userData, projectData, logData] = await Promise.all([
        projectService.getCustomers(),
        projectService.getUsers(),
        projectService.getProjects(),
        projectService.getAuditLogs(),
      ])

      const activeProjectIds = new Set(
        projectData
          .filter(project => project.isActive !== 0)
          .map(project => project.projectId),
      )

      const edits = userData.reduce((acc, user) => {
        acc[user.userId] = {
          fullName: user.fullName || '',
          email: user.email || '',
          role: user.userRole || user.role || user.Role || roleOptions[0],
          status: user.userStatus || user.status || user.Status || userStatusOptions[0],
          password: '',
          projectIds: (user.projectIds || []).filter(projectId => activeProjectIds.has(projectId)),
        }
        return acc
      }, {})

      setCustomers(customerData)
      setUsers(userData)
      setProjects(projectData)
      setAuditLogs(logData)
      setUserEdits(edits)
    } catch (loadError) {
      showError(loadError, 'Admin paneli verileri yüklenirken hata oluştu. Lütfen tekrar deneyin.')
    } finally {
      setLoading(false)
    }
  }

  const loadAuditLogs = async () => {
    setLogsLoading(true)
    setError('')

    try {
      setAuditLogs(await projectService.getAuditLogs())
    } catch (loadError) {
      showError(loadError, 'Loglar yenilenirken hata oluştu.')
    } finally {
      setLogsLoading(false)
    }
  }

  const loadInitialAdminData = useEffectEvent(() => {
    void loadAdminData()
  })

  useEffect(() => {
    if (!isAdmin) return

    const timeoutId = window.setTimeout(loadInitialAdminData, 0)
    return () => window.clearTimeout(timeoutId)
  }, [isAdmin])

  const handleChangeCustomer = (field, value) => {
    setNewCustomer(prev => ({ ...prev, [field]: value }))
  }

  const handleChangeNewUser = (field, value) => {
    setNewUser(prev => ({ ...prev, [field]: value }))
  }

  const handleUserEditChange = (userId, field, value) => {
    setUserEdits(prev => ({
      ...prev,
      [userId]: {
        ...prev[userId],
        [field]: value,
      },
    }))
  }

  const handleProjectToggle = (userId, projectId) => {
    setUserEdits(prev => {
      const currentProjectIds = prev[userId]?.projectIds || []
      const projectIds = currentProjectIds.includes(projectId)
        ? currentProjectIds.filter(id => id !== projectId)
        : [...currentProjectIds, projectId]

      return {
        ...prev,
        [userId]: {
          ...prev[userId],
          projectIds,
        },
      }
    })
  }

  const handleClearRequest = (requestId) => {
    removePasswordChangeRequest(requestId)
    showSuccess('Şifre değiştirme bildirimi temizlendi.')
  }

  const handleClearAllRequests = () => {
    if (!window.confirm('Tüm şifre değiştirme bildirimlerini temizlemek istiyor musunuz?')) return

    clearPasswordChangeRequests()
    showSuccess('Tüm şifre değiştirme bildirimleri temizlendi.')
  }

  const handleEditCustomer = (customer) => {
    setEditingCustomerId(customer.customerId)
    setNewCustomer({
      CustomerName: customer.customerName,
      CustomerType: customer.customerType,
      CustomerStatus: customer.customerStatus,
    })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const handleCancelCustomerEdit = () => {
    setEditingCustomerId(null)
    setNewCustomer({
      CustomerName: '',
      CustomerType: customerTypeOptions[0],
      CustomerStatus: customerStatusOptions[0],
    })
  }

  const handleSaveCustomer = async (event) => {
    event.preventDefault()
    setLoading(true)
    setError('')
    setMessage('')

    try {
      if (editingCustomerId) {
        await projectService.updateCustomer(editingCustomerId, newCustomer)
        showSuccess('Müşteri başarıyla güncellendi.')
      } else {
        await projectService.createCustomer(newCustomer)
        showSuccess('Müşteri başarıyla oluşturuldu.')
      }

      handleCancelCustomerEdit()
      await loadAdminData()
    } catch (saveError) {
      showError(saveError, 'Müşteri kaydedilirken hata oluştu. Lütfen alanları kontrol edin.')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteCustomer = async (id) => {
    if (!window.confirm('Müşteriyi silmek istediğinizden emin misiniz?')) return

    setLoading(true)
    setError('')
    setMessage('')

    try {
      await projectService.deleteCustomer(id)
      showSuccess('Müşteri başarıyla silindi.')
      await loadAdminData()
    } catch (deleteError) {
      showError(deleteError, 'Müşteri silinirken hata oluştu.')
    } finally {
      setLoading(false)
    }
  }

  const handleCreateUser = async (event) => {
    event.preventDefault()
    setLoading(true)
    setError('')
    setMessage('')

    try {
      await projectService.createUser(newUser)
      setNewUser({
        Email: '',
        FullName: '',
        Role: roleOptions[1],
        Password: '',
      })
      showSuccess('Kullanıcı başarıyla oluşturuldu.')
      await loadAdminData()
    } catch (createError) {
      showError(createError, 'Kullanıcı oluşturulurken hata oluştu. Lütfen bilgileri kontrol edin.')
    } finally {
      setLoading(false)
    }
  }

  const handleUpdateUser = async (event, id) => {
    event.preventDefault()
    const updatePayload = userEdits[id]
    if (!updatePayload) return

    setLoading(true)
    setError('')
    setMessage('')

    try {
      await projectService.updateUser(id, {
        Email: updatePayload.email,
        FullName: updatePayload.fullName,
        Role: updatePayload.role,
        Status: updatePayload.status,
        Password: updatePayload.password.trim() || null,
        ProjectIds: updatePayload.projectIds,
      })
      showSuccess('Kullanıcı bilgileri ve yetkileri başarıyla güncellendi.')
      await loadAdminData()
    } catch (updateError) {
      showError(updateError, 'Kullanıcı güncellenirken hata oluştu.')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteUser = async (id) => {
    if (!window.confirm('Kullanıcıyı silmek istediğinizden emin misiniz?')) return

    setLoading(true)
    setError('')
    setMessage('')

    try {
      await projectService.deleteUser(id)
      showSuccess('Kullanıcı başarıyla silindi.')
      await loadAdminData()
    } catch (deleteError) {
      showError(deleteError, 'Kullanıcı silinirken hata oluştu.')
    } finally {
      setLoading(false)
    }
  }

  const activeProjects = projects.filter(project => project.isActive !== 0)
  const normalizedLogSearch = logSearch.trim().toLocaleLowerCase('tr-TR')
  const filteredAuditLogs = auditLogs.filter((log) => {
    if (logActionFilter && log.actionType !== logActionFilter) return false
    if (!normalizedLogSearch) return true

    return [
      log.userFullName,
      log.userId,
      log.entityName,
      entityLabels[log.entityName],
      log.entityId,
      log.actionType,
      actionLabels[log.actionType],
      log.oldValues,
      log.newValues,
    ]
      .filter(Boolean)
      .some(value => String(value).toLocaleLowerCase('tr-TR').includes(normalizedLogSearch))
  })

  return (
    <div className="settings-page page-content">
      {!isAdmin ? (
        <div className="dashboard-card full-width settings-access-warning">
          <h3>Erişim Reddedildi</h3>
          <p>Bu panel yalnızca Sistem Yöneticisi rolü tarafından görüntülenebilir.</p>
        </div>
      ) : (
        <>
          <section className="admin-hero">
            <div className="admin-hero-copy">
              <span className="admin-hero-kicker">Sistem Yönetimi</span>
              <h2>Admin Kontrol Merkezi</h2>
              <p>Kullanıcıları, erişimleri, bildirimleri ve sistem hareketlerini tek noktadan yönetin.</p>
            </div>
            <div className="admin-hero-stats" aria-label="Sistem özeti">
              <div className="admin-hero-stat">
                <strong>{users.length}</strong>
                <span>Kullanıcı</span>
              </div>
              <div className="admin-hero-stat">
                <strong>{activeProjects.length}</strong>
                <span>Aktif Proje</span>
              </div>
            </div>
          </section>

          {message && <div className={`settings-message ${messageType}`}>{message}</div>}
          {error && <div className="settings-error">{error}</div>}

          <nav className="settings-tabs" role="tablist" aria-label="Admin paneli bölümleri">
            {tabs.map(tab => (
              <button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={activeTab === tab.id}
                className={`settings-tab${activeTab === tab.id ? ' active' : ''}`}
                onClick={() => setActiveTab(tab.id)}
              >
                <span>{tab.label}</span>
                {tab.id === 'notifications' && passwordChangeRequests.length > 0 && (
                  <strong>{passwordChangeRequests.length}</strong>
                )}
                {tab.id === 'logs' && auditLogs.length > 0 && (
                  <small>{auditLogs.length}</small>
                )}
              </button>
            ))}
          </nav>

          {activeTab === 'notifications' && (
            <section className="dashboard-card full-width settings-panel">
              <div className="settings-card-header settings-card-header-with-action">
                <div>
                  <h3>Şifre Değiştirme İstekleri</h3>
                  <span>Giriş ekranından gelen şifre değiştirme taleplerini buradan takip edebilirsiniz.</span>
                </div>
                {passwordChangeRequests.length > 0 && (
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={handleClearAllRequests}
                  >
                    Tümünü Temizle
                  </button>
                )}
              </div>

              {passwordChangeRequests.length === 0 ? (
                <div className="settings-empty-state">
                  <strong>Bekleyen bildirim yok</strong>
                  <span>Yeni bir şifre değiştirme isteği geldiğinde burada görünecek.</span>
                </div>
              ) : (
                <ul className="password-request-list">
                  {passwordChangeRequests.map(request => (
                    <li key={request.id} className="password-request-item">
                      <div className="password-request-avatar">{getInitials(request.fullName) || '?'}</div>
                      <div className="password-request-content">
                        <strong>{request.fullName}</strong>
                        <span>{request.email}</span>
                        <small>{new Date(request.requestedAt).toLocaleString('tr-TR')}</small>
                        <p>{request.message}</p>
                      </div>
                      <button
                        type="button"
                        className="request-clear-button"
                        onClick={() => handleClearRequest(request.id)}
                      >
                        Temizle
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          )}

          {activeTab === 'users' && (
            <div className="settings-panel-stack">
              <section className="dashboard-card settings-panel">
                <div className="settings-card-header">
                  <h3>Yeni Kullanıcı</h3>
                  <span>Yeni hesap oluşturun; proje erişimlerini oluşturduktan sonra aşağıdan atayın.</span>
                </div>

                <form className="settings-form" onSubmit={handleCreateUser}>
                  <div className="settings-form-grid">
                    <div className="settings-form-row">
                      <label htmlFor="userFullName">Ad Soyad</label>
                      <input
                        id="userFullName"
                        value={newUser.FullName}
                        onChange={event => handleChangeNewUser('FullName', event.target.value)}
                        placeholder="Ad Soyad"
                        required
                      />
                    </div>

                    <div className="settings-form-row">
                      <label htmlFor="userEmail">E-posta</label>
                      <input
                        id="userEmail"
                        type="email"
                        value={newUser.Email}
                        onChange={event => handleChangeNewUser('Email', event.target.value)}
                        placeholder="ornek@sirket.com"
                        required
                      />
                    </div>

                    <div className="settings-form-row">
                      <label htmlFor="userRole">Sistem Rolü</label>
                      <select
                        id="userRole"
                        value={newUser.Role}
                        onChange={event => handleChangeNewUser('Role', event.target.value)}
                      >
                        {roleOptions.map(role => (
                          <option key={role} value={role}>{role}</option>
                        ))}
                      </select>
                    </div>

                    <div className="settings-form-row">
                      <label htmlFor="userPassword">Geçici Şifre</label>
                      <input
                        id="userPassword"
                        type="password"
                        minLength="8"
                        value={newUser.Password}
                        onChange={event => handleChangeNewUser('Password', event.target.value)}
                        placeholder="En az 8 karakter"
                        required
                      />
                    </div>
                  </div>

                  <button className="primary-button settings-submit-button" type="submit" disabled={loading}>
                    Kullanıcı Oluştur
                  </button>
                </form>
              </section>

              <section className="dashboard-card settings-panel">
                <div className="settings-card-header">
                  <h3>Kullanıcılar ve Yetkiler</h3>
                  <span>Bir kullanıcıyı açarak profilini, şifresini, rolünü ve proje erişimlerini yönetin.</span>
                </div>

                {users.length === 0 ? (
                  <div className="settings-empty-state">
                    <strong>Henüz kullanıcı bulunamadı</strong>
                  </div>
                ) : (
                  <div className="user-admin-list">
                    {users.map(user => {
                      const edit = userEdits[user.userId] || {}
                      const assignedCount = edit.projectIds?.length || 0
                      const managedProjects = activeProjects.filter(
                        project => project.projectManagerUserId === user.userId,
                      )

                      return (
                        <details className="user-admin-card" key={user.userId}>
                          <summary>
                            <div className="user-avatar">{getInitials(user.fullName) || '?'}</div>
                            <div className="user-summary-copy">
                              <strong>{user.fullName}</strong>
                              <span>{user.email}</span>
                            </div>
                            <div className="user-summary-meta">
                              <span className={`status-pill ${edit.status === 'Pasif' ? 'inactive' : 'active'}`}>
                                {edit.status || user.status}
                              </span>
                              <span>{assignedCount} proje erişimi</span>
                            </div>
                          </summary>

                          <form className="user-edit-form" onSubmit={event => handleUpdateUser(event, user.userId)}>
                            <div className="settings-form-grid user-profile-grid">
                              <div className="settings-form-row">
                                <label htmlFor={`fullName-${user.userId}`}>Ad Soyad</label>
                                <input
                                  id={`fullName-${user.userId}`}
                                  value={edit.fullName || ''}
                                  onChange={event => handleUserEditChange(user.userId, 'fullName', event.target.value)}
                                  required
                                />
                              </div>

                              <div className="settings-form-row">
                                <label htmlFor={`email-${user.userId}`}>E-posta</label>
                                <input
                                  id={`email-${user.userId}`}
                                  type="email"
                                  value={edit.email || ''}
                                  onChange={event => handleUserEditChange(user.userId, 'email', event.target.value)}
                                  required
                                />
                              </div>

                              <div className="settings-form-row">
                                <label htmlFor={`role-${user.userId}`}>Sistem Rolü</label>
                                <select
                                  id={`role-${user.userId}`}
                                  value={edit.role || roleOptions[0]}
                                  onChange={event => handleUserEditChange(user.userId, 'role', event.target.value)}
                                >
                                  {roleOptions.map(role => (
                                    <option key={role} value={role}>{role}</option>
                                  ))}
                                </select>
                              </div>

                              <div className="settings-form-row">
                                <label htmlFor={`status-${user.userId}`}>Hesap Durumu</label>
                                <select
                                  id={`status-${user.userId}`}
                                  value={edit.status || userStatusOptions[0]}
                                  onChange={event => handleUserEditChange(user.userId, 'status', event.target.value)}
                                >
                                  {userStatusOptions.map(status => (
                                    <option key={status} value={status}>{status}</option>
                                  ))}
                                </select>
                              </div>

                              <div className="settings-form-row user-password-field">
                                <label htmlFor={`password-${user.userId}`}>Yeni Şifre</label>
                                <input
                                  id={`password-${user.userId}`}
                                  type="password"
                                  minLength="8"
                                  value={edit.password || ''}
                                  onChange={event => handleUserEditChange(user.userId, 'password', event.target.value)}
                                  placeholder="Değiştirmek istemiyorsanız boş bırakın"
                                />
                              </div>
                            </div>

                            <fieldset className="project-access-fieldset">
                              <legend>Proje Erişimleri</legend>
                              <p>Seçilen projeler kullanıcıya görüntüleme ve rolünün izin verdiği işlemleri yapma erişimi verir.</p>

                              {managedProjects.length > 0 && (
                                <div className="managed-project-note">
                                  Bu kullanıcı {managedProjects.map(project => project.projectCode).join(', ')} projelerinde
                                  proje yöneticisidir ve bu projelere otomatik erişir.
                                </div>
                              )}

                              {activeProjects.length === 0 ? (
                                <span className="project-access-empty">Atanabilecek aktif proje bulunamadı.</span>
                              ) : (
                                <div className="project-checkbox-grid">
                                  {activeProjects.map(project => (
                                    <label className="project-checkbox" key={project.projectId}>
                                      <input
                                        type="checkbox"
                                        checked={edit.projectIds?.includes(project.projectId) || false}
                                        onChange={() => handleProjectToggle(user.userId, project.projectId)}
                                      />
                                      <span>
                                        <strong>{project.projectCode}</strong>
                                        <small>{project.projectName}</small>
                                      </span>
                                    </label>
                                  ))}
                                </div>
                              )}
                            </fieldset>

                            <div className="user-edit-actions">
                              <button className="primary-button" type="submit" disabled={loading}>
                                Değişiklikleri Kaydet
                              </button>
                              <button
                                className="danger-button"
                                type="button"
                                onClick={() => handleDeleteUser(user.userId)}
                                disabled={loading}
                              >
                                Kullanıcıyı Sil
                              </button>
                            </div>
                          </form>
                        </details>
                      )
                    })}
                  </div>
                )}
              </section>
            </div>
          )}

          {activeTab === 'customers' && (
            <section className="dashboard-card settings-panel">
              <div className="settings-card-header">
                <h3>Müşteri Yönetimi</h3>
                <span>Yeni müşteri ekleyin veya mevcut müşteri bilgilerini düzenleyin.</span>
              </div>

              <form className="settings-form" onSubmit={handleSaveCustomer}>
                <div className="settings-form-grid">
                  <div className="settings-form-row">
                    <label htmlFor="customerName">Müşteri Adı</label>
                    <input
                      id="customerName"
                      value={newCustomer.CustomerName}
                      onChange={event => handleChangeCustomer('CustomerName', event.target.value)}
                      placeholder="Müşteri adı girin"
                      required
                    />
                  </div>

                  <div className="settings-form-row">
                    <label htmlFor="customerType">Müşteri Türü</label>
                    <select
                      id="customerType"
                      value={newCustomer.CustomerType}
                      onChange={event => handleChangeCustomer('CustomerType', event.target.value)}
                    >
                      {customerTypeOptions.map(type => (
                        <option key={type} value={type}>{type}</option>
                      ))}
                    </select>
                  </div>

                  <div className="settings-form-row">
                    <label htmlFor="customerStatus">Durum</label>
                    <select
                      id="customerStatus"
                      value={newCustomer.CustomerStatus}
                      onChange={event => handleChangeCustomer('CustomerStatus', event.target.value)}
                    >
                      {customerStatusOptions.map(status => (
                        <option key={status} value={status}>{status}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="settings-form-actions">
                  <button className="primary-button" type="submit" disabled={loading}>
                    {editingCustomerId ? 'Müşteriyi Güncelle' : 'Müşteri Oluştur'}
                  </button>
                  {editingCustomerId && (
                    <button
                      className="secondary-button"
                      type="button"
                      onClick={handleCancelCustomerEdit}
                      disabled={loading}
                    >
                      İptal
                    </button>
                  )}
                </div>
              </form>

              <div className="settings-table-wrapper">
                <table className="settings-table">
                  <thead>
                    <tr>
                      <th>Adı</th>
                      <th>Tür</th>
                      <th>Durum</th>
                      <th>Aksiyon</th>
                    </tr>
                  </thead>
                  <tbody>
                    {customers.length === 0 ? (
                      <tr>
                        <td colSpan="4">Henüz müşteri bulunamadı.</td>
                      </tr>
                    ) : (
                      customers.map(customer => (
                        <tr key={customer.customerId}>
                          <td>{customer.customerName}</td>
                          <td>{customer.customerType}</td>
                          <td>{customer.customerStatus}</td>
                          <td>
                            <div className="settings-action-cell">
                              <button
                                className="edit-button"
                                type="button"
                                onClick={() => handleEditCustomer(customer)}
                                disabled={loading}
                              >
                                Düzenle
                              </button>
                              <button
                                className="danger-button"
                                type="button"
                                onClick={() => handleDeleteCustomer(customer.customerId)}
                                disabled={loading}
                              >
                                Sil
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {activeTab === 'logs' && (
            <section className="dashboard-card full-width settings-panel audit-panel">
              <div className="settings-card-header settings-card-header-with-action">
                <div>
                  <h3>Sistem Logları</h3>
                  <span>Kullanıcıların sistemde oluşturduğu, güncellediği ve sildiği tüm kayıtlar.</span>
                </div>
                <button
                  type="button"
                  className="secondary-button"
                  onClick={loadAuditLogs}
                  disabled={logsLoading}
                >
                  {logsLoading ? 'Yenileniyor...' : 'Logları Yenile'}
                </button>
              </div>

              <div className="audit-toolbar">
                <label>
                  <span>Loglarda ara</span>
                  <input
                    type="search"
                    value={logSearch}
                    onChange={event => setLogSearch(event.target.value)}
                    placeholder="Kullanıcı, kayıt veya işlem ara"
                  />
                </label>
                <label>
                  <span>İşlem türü</span>
                  <select
                    value={logActionFilter}
                    onChange={event => setLogActionFilter(event.target.value)}
                  >
                    <option value="">Tüm işlemler</option>
                    <option value="INSERT">Oluşturma</option>
                    <option value="UPDATE">Güncelleme</option>
                    <option value="DELETE">Silme</option>
                  </select>
                </label>
                <div className="audit-result-count">
                  <strong>{filteredAuditLogs.length}</strong>
                  <span>kayıt gösteriliyor</span>
                </div>
              </div>

              <div className="settings-table-wrapper">
                <table className="settings-table audit-table">
                  <thead>
                    <tr>
                      <th>Tarih</th>
                      <th>Kullanıcı</th>
                      <th>İşlem</th>
                      <th>Kayıt</th>
                      <th>Değişiklik</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredAuditLogs.length === 0 ? (
                      <tr>
                        <td colSpan="5">Filtreye uygun log kaydı bulunamadı.</td>
                      </tr>
                    ) : (
                      filteredAuditLogs.map(log => (
                        <tr key={log.auditLogId}>
                          <td className="audit-date-cell">
                            {new Date(log.changedAt).toLocaleString('tr-TR')}
                          </td>
                          <td>
                            <strong>{log.userFullName || 'Sistem'}</strong>
                            <small>{log.userId || log.ipAddress || 'Otomatik işlem'}</small>
                          </td>
                          <td>
                            <span className={`audit-action ${log.actionType.toLowerCase()}`}>
                              {actionLabels[log.actionType] || log.actionType}
                            </span>
                          </td>
                          <td>
                            <strong>{entityLabels[log.entityName] || log.entityName}</strong>
                            <small>{log.entityId}</small>
                          </td>
                          <td>
                            <details className="audit-details">
                              <summary>Değişiklikleri göster</summary>
                              <div className="audit-values">
                                <section>
                                  <strong>Önceki Değer</strong>
                                  <pre>{formatAuditValues(log.oldValues)}</pre>
                                </section>
                                <section>
                                  <strong>Yeni Değer</strong>
                                  <pre>{formatAuditValues(log.newValues)}</pre>
                                </section>
                              </div>
                            </details>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          )}
        </>
      )}
    </div>
  )
}

export default SettingsPage
