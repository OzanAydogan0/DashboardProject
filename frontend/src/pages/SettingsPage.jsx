import { useEffect, useState } from 'react'
import './SettingsPage.css'
import { projectService } from '../services/projectService'
import { useAlert } from '../components/AlertProvider'
import { getPasswordChangeRequests } from '../utils/adminNotifications'

const roleOptions = [
  'Sistem Yöneticisi',
  'Proje Yöneticisi',
  'Üst Yönetim İzleyicisi',
  'Üst Yönetim',
]

const userStatusOptions = ['Aktif', 'Pasif']
const customerTypeOptions = ['Kurumsal', 'Hükümet', 'Sivil Toplum', 'Diğer']
const customerStatusOptions = ['Aktif', 'Pasif']

function SettingsPage() {
  const { addAlert } = useAlert()
  const [isAdmin, setIsAdmin] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [messageType, setMessageType] = useState('success')
  const [customers, setCustomers] = useState([])
  const [users, setUsers] = useState([])
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
  const [passwordChangeRequests, setPasswordChangeRequests] = useState([])

  useEffect(() => {
    const userString = localStorage.getItem('user')
    if (!userString) return

    try {
      const storedUser = JSON.parse(userString)
      const role = storedUser?.userRole || storedUser?.UserRole || storedUser?.role || storedUser?.Role
      setIsAdmin(role === 'Sistem Yöneticisi')
    } catch {
      setIsAdmin(false)
    }
  }, [])

  useEffect(() => {
    if (!isAdmin) return
    loadAdminData()
  }, [isAdmin])

  useEffect(() => {
    setPasswordChangeRequests(getPasswordChangeRequests())

    const handleNewRequest = () => {
      setPasswordChangeRequests(getPasswordChangeRequests())
    }

    window.addEventListener('password-change-request', handleNewRequest)
    return () => window.removeEventListener('password-change-request', handleNewRequest)
  }, [])

  const loadAdminData = async () => {
    setLoading(true)
    setError('')

    try {
      const [customerData, userData] = await Promise.all([
        projectService.getCustomers(),
        projectService.getUsers(),
      ])

      setCustomers(customerData)
      setUsers(userData)

      const edits = userData.reduce((acc, user) => {
        acc[user.userId] = {
          role: user.userRole || user.role || user.Role || roleOptions[0],
          status: user.userStatus || user.status || user.Status || userStatusOptions[0],
        }
        return acc
      }, {})

      setUserEdits(edits)
    } catch (loadError) {
      setError('Veriler yüklenirken hata oluştu. Lütfen tekrar deneyin.')
    } finally {
      setLoading(false)
    }
  }

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
        setMessage('Müşteri başarıyla güncellendi.')
        addAlert('Müşteri başarıyla güncellendi.', 'success')
      } else {
        await projectService.createCustomer(newCustomer)
        setMessage('Müşteri başarıyla oluşturuldu.')
        addAlert('Müşteri başarıyla oluşturuldu.', 'success')
      }
      setMessageType('success')
      handleCancelCustomerEdit()
      window.scrollTo({ top: 0, behavior: 'smooth' })
      await loadAdminData()
    } catch (createError) {
      const serverMessage = createError?.response?.data?.message
      const errorMessage = serverMessage || 'Müşteri kaydedilirken hata oluştu. Lütfen alanları kontrol edin.'
      setError(errorMessage)
      setMessage('')
      addAlert(errorMessage, 'error')
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
      setMessage('Müşteri başarıyla silindi.')
      setMessageType('success')
      addAlert('Müşteri başarıyla silindi.', 'success')
      await loadAdminData()
    } catch (deleteError) {
      const serverMessage = deleteError?.response?.data?.message
      const errorMessage = serverMessage || 'Müşteri silinirken hata oluştu.'
      setError(errorMessage)
      addAlert(errorMessage, 'error')
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
      setMessage('Kullanıcı başarıyla oluşturuldu.')
      setMessageType('success')
      addAlert('Kullanıcı başarıyla oluşturuldu.', 'success')
      await loadAdminData()
    } catch (createError) {
      const serverMessage = createError?.response?.data?.message
      const errorMessage = serverMessage || 'Kullanıcı oluşturulurken hata oluştu. Lütfen bilgileri kontrol edin.'
      setError(errorMessage)
      addAlert(errorMessage, 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleUpdateUser = async (id) => {
    const updatePayload = userEdits[id]
    if (!updatePayload) return

    setLoading(true)
    setError('')
    setMessage('')

    try {
      await projectService.updateUser(id, {
        Role: updatePayload.role,
        Status: updatePayload.status,
      })
      setMessage('Kullanıcı başarıyla güncellendi.')
      setMessageType('success')
      addAlert('Kullanıcı başarıyla güncellendi.', 'success')
      await loadAdminData()
    } catch (updateError) {
      const serverMessage = updateError?.response?.data?.message
      const errorMessage = serverMessage || 'Kullanıcı güncellenirken hata oluştu.'
      setError(errorMessage)
      addAlert(errorMessage, 'error')
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
      setMessage('Kullanıcı başarıyla silindi.')
      setMessageType('success')
      addAlert('Kullanıcı başarıyla silindi.', 'success')
      await loadAdminData()
    } catch (deleteError) {
      const serverMessage = deleteError?.response?.data?.message
      const errorMessage = serverMessage || 'Kullanıcı silinirken hata oluştu.'
      setError(errorMessage)
      addAlert(errorMessage, 'error')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="settings-page page-content">
      {!isAdmin ? (
        <div className="dashboard-card full-width settings-access-warning">
          <h3>Erişim Reddedildi</h3>
          <p>Bu panel yalnızca Sistem Yöneticisi rolleri tarafından görüntülenebilir.</p>
        </div>
      ) : (
        <>
          {message && <div className={`settings-message ${messageType}`}>{message}</div>}
          {error && <div className="settings-error">{error}</div>}

          <div className="dashboard-card full-width">
            <div className="settings-card-header">
              <h3>Şifre Değiştirme İstekleri</h3>
              <span>Giriş ekranından gelen şifre değiştirme taleplerini buradan takip edebilirsiniz.</span>
            </div>

            {passwordChangeRequests.length === 0 ? (
              <p>Henüz şifre değiştirme isteği yok.</p>
            ) : (
              <ul className="password-request-list">
                {passwordChangeRequests.map((request) => (
                  <li key={request.id} className="password-request-item">
                    <strong>{request.fullName}</strong>
                    <span>{request.email}</span>
                    <small>{new Date(request.requestedAt).toLocaleString('tr-TR')}</small>
                    <p>{request.message}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="settings-grid">
            <section className="dashboard-card settings-card">
              <div className="settings-card-header">
                <h3>Müşteri Yönetimi</h3>
                <span>Yeni müşteri ekleyebilir ve mevcut müşterileri silebilirsiniz.</span>
              </div>

              <form className="settings-form" onSubmit={handleSaveCustomer}>
                <div className="settings-form-row">
                  <label htmlFor="customerName">Müşteri Adı</label>
                  <input
                    id="customerName"
                    value={newCustomer.CustomerName}
                    onChange={(e) => handleChangeCustomer('CustomerName', e.target.value)}
                    placeholder="Müşteri adı girin"
                    required
                  />
                </div>

                <div className="settings-form-row">
                  <label htmlFor="customerType">Müşteri Türü</label>
                  <select
                    id="customerType"
                    value={newCustomer.CustomerType}
                    onChange={(e) => handleChangeCustomer('CustomerType', e.target.value)}
                  >
                    {customerTypeOptions.map((type) => (
                      <option key={type} value={type}>{type}</option>
                    ))}
                  </select>
                </div>

                <div className="settings-form-row">
                  <label htmlFor="customerStatus">Durum</label>
                  <select
                    id="customerStatus"
                    value={newCustomer.CustomerStatus}
                    onChange={(e) => handleChangeCustomer('CustomerStatus', e.target.value)}
                  >
                    {customerStatusOptions.map((status) => (
                      <option key={status} value={status}>{status}</option>
                    ))}
                  </select>
                </div>

                <div className="settings-form-actions">
                  <button className="primary-button" type="submit" disabled={loading}>
                    {editingCustomerId ? 'Müşteri Güncelle' : 'Müşteri Oluştur'}
                  </button>
                  {editingCustomerId && (
                    <button className="secondary-button" type="button" onClick={handleCancelCustomerEdit} disabled={loading}>
                      İptal
                    </button>
                  )}
                </div>
              </form>

              <div className="settings-table-wrapper">
                <h4>Mevcut Müşteriler</h4>
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
                      customers.map((customer) => (
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

            <section className="dashboard-card settings-card">
              <div className="settings-card-header">
                <h3>Kullanıcı Yönetimi</h3>
                <span>Kullanıcı ekleyebilir, rollerini güncelleyebilir ya da silebilirsiniz.</span>
              </div>

              <form className="settings-form" onSubmit={handleCreateUser}>
                <div className="settings-form-row">
                  <label htmlFor="userFullName">Tam Adı</label>
                  <input
                    id="userFullName"
                    value={newUser.FullName}
                    onChange={(e) => handleChangeNewUser('FullName', e.target.value)}
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
                    onChange={(e) => handleChangeNewUser('Email', e.target.value)}
                    placeholder="e-posta adresi"
                    required
                  />
                </div>

                <div className="settings-form-row">
                  <label htmlFor="userRole">Rol</label>
                  <select
                    id="userRole"
                    value={newUser.Role}
                    onChange={(e) => handleChangeNewUser('Role', e.target.value)}
                  >
                    {roleOptions.map((role) => (
                      <option key={role} value={role}>{role}</option>
                    ))}
                  </select>
                </div>

                <div className="settings-form-row">
                  <label htmlFor="userPassword">Şifre</label>
                  <input
                    id="userPassword"
                    type="password"
                    value={newUser.Password}
                    onChange={(e) => handleChangeNewUser('Password', e.target.value)}
                    placeholder="Güçlü bir şifre girin"
                    required
                  />
                </div>

                <button className="primary-button" type="submit" disabled={loading}>
                  Kullanıcı Oluştur
                </button>
              </form>

              <div className="settings-table-wrapper">
                <h4>Mevcut Kullanıcılar</h4>
                <table className="settings-table">
                  <thead>
                    <tr>
                      <th>Ad Soyad</th>
                      <th>E-posta</th>
                      <th>Rol</th>
                      <th>Durum</th>
                      <th>Aksiyon</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.length === 0 ? (
                      <tr>
                        <td colSpan="5">Henüz kullanıcı bulunamadı.</td>
                      </tr>
                    ) : (
                      users.map((user) => {
                        const edit = userEdits[user.userId] || {}
                        const currentRole = edit.role || user.userRole || roleOptions[0]
                        const currentStatus = edit.status || user.userStatus || userStatusOptions[0]

                        return (
                          <tr key={user.userId}>
                            <td>{user.fullName}</td>
                            <td>{user.email}</td>
                            <td>
                              <select
                                value={currentRole}
                                onChange={(e) => handleUserEditChange(user.userId, 'role', e.target.value)}
                              >
                                {roleOptions.map((role) => (
                                  <option key={role} value={role}>{role}</option>
                                ))}
                              </select>
                            </td>
                            <td>
                              <select
                                value={currentStatus}
                                onChange={(e) => handleUserEditChange(user.userId, 'status', e.target.value)}
                              >
                                {userStatusOptions.map((status) => (
                                  <option key={status} value={status}>{status}</option>
                                ))}
                              </select>
                            </td>
                            <td className="settings-action-cell">
                              <button
                                type="button"
                                className="edit-button"
                                onClick={() => handleUpdateUser(user.userId)}
                                disabled={loading}
                              >
                                Düzenle
                              </button>
                              <button
                                type="button"
                                className="danger-button"
                                onClick={() => handleDeleteUser(user.userId)}
                                disabled={loading}
                              >
                                Sil
                              </button>
                            </td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        </>
      )}
    </div>
  )
}

export default SettingsPage
