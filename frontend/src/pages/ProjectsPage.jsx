import { useMemo, useState, useEffect, useRef, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { projectService } from '../services/projectService'
import { useAlert } from '../components/AlertProvider'
import { canCreateProject, canEditProject, isSystemAdmin } from '../utils/permissionHelper'
import './ProjectsPage.css'

// Filtre Seçenekleri
const progressOptions = ['Hepsi', 'Taslak', 'Aktif', 'Beklemede', 'Tamamlandı', 'Pasif']
const healthOptions = ['Hepsi', 'Yeşil', 'Sarı', 'Kırmızı', 'Gri']
const activeOptions = ['Hepsi', 'Devam Ediyor', 'İptal Edildi']
const budgetSortOptions = ['Yok', 'Artan', 'Azalan']
const finishSortOptions = ['Yok', 'Artan', 'Azalan']

// Form Seçenekleri (Modal İçi)
const statusFormOptions = ['Taslak', 'Aktif', 'Beklemede', 'Tamamlandı', 'Pasif']
const healthFormOptions = ['Yeşil', 'Sarı', 'Kırmızı', 'Gri']
const currencyOptions = ['TRY', 'USD', 'EUR']

// 🎨 Rozet Stilleri
const getHealthBadgeStyle = (health) => {
  switch (health) {
    case 'Yeşil': return { bg: '#d1fae5', text: '#065f46' }
    case 'Sarı': return { bg: '#fef3c7', text: '#92400e' }
    case 'Kırmızı': return { bg: '#fee2e2', text: '#991b1b' }
    case 'Gri': return { bg: '#f3f4f6', text: '#4b5563' }
    default: return { bg: '#f3f4f6', text: '#374151' }
  }
}

const getProgressBadgeStyle = (status) => {
  switch (status) {
    case 'Tamamlandı': return { bg: '#d1fae5', text: '#065f46' }
    case 'Aktif':
    case 'Devam Ediyor': return { bg: '#dbeafe', text: '#1e40af' }
    case 'Taslak':
    case 'Planlandı': return { bg: '#f3f4f6', text: '#374151' }
    case 'Gecikti': return { bg: '#fee2e2', text: '#991b1b' }
    default: return { bg: '#f3f4f6', text: '#374151' }
  }
}

const getProjectFinishDate = (project) => {
  const rawDate = project.forecastFinishDate || project.baselineFinishDate
  if (!rawDate) return ''
  return rawDate.split('T')[0]
}

const getProjectIsActiveFlag = (project) => {
  const rawValue = project.isActive ?? project.IsActive
  if (rawValue === undefined || rawValue === null || rawValue === '') return 1
  if (typeof rawValue === 'boolean') return rawValue ? 1 : 0
  const parsedNumber = Number(rawValue)
  if (!Number.isNaN(parsedNumber)) return parsedNumber
  const normalized = String(rawValue).trim().toLowerCase()
  if (['0', 'false', 'pasif', 'iptal', 'inactive'].includes(normalized)) return 0
  return 1
}

const getActiveLabel = (project) => {
  return getProjectIsActiveFlag(project) === 0 ? 'İptal Edildi' : 'Devam Ediyor'
}

// Boş Form Başlangıç Durumu
const initialFormState = {
  projectCode: '',
  projectName: '',
  projectDescription: '',
  projectStatus: 'Taslak',
  manualHealth: 'Yeşil',
  plannedProgress: 0,
  actualProgress: 0,
  bac: 0,
  currency: 'TRY',
  startDate: '',
  baselineFinishDate: '',
  forecastFinishDate: '',
  programId: '',
  customerId: '',
  projectManagerUserId: '',
  confidentiality: 'Şirket İçi',
  isActive: 1
}

function ProjectsPage() {
  const navigate = useNavigate()
  const fileInputRef = useRef(null)
  const { addAlert } = useAlert()

  const [rawProjects, setRawProjects] = useState([])
  const [programs, setPrograms] = useState([])
  const [customers, setCustomers] = useState([])
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  
  // 📊 Excel Import State
  const [isImporting, setIsImporting] = useState(false)
  const canCreate = canCreateProject()
  const canImport = isSystemAdmin()

  // --- MODAL & FORM STATE'LERİ ---
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingProjectId, setEditingProjectId] = useState(null)
  const [formData, setFormData] = useState(initialFormState)
  const [formSubmitting, setFormSubmitting] = useState(false)
  const [modalError, setModalError] = useState('')

  // Filtre State'leri
  const [draftFilters, setDraftFilters] = useState({
    name: '',
    code: '',
    progress: 'Hepsi',
    health: 'Hepsi',
    isActive: 'Hepsi',
    manager: '',
    budgetSort: 'Yok',
    finish: '',
    finishSort: 'Yok',
  })
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 10

  const formatCurrency = (value, currency = 'TRY') => {
    if (value === null || value === undefined || value === '') return '-'
    const amount = Number(value)
    if (Number.isNaN(amount)) return '-'
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      maximumFractionDigits: 0,
    }).format(amount)
  }
  const [filters, setFilters] = useState(draftFilters)

  // 🔄 Projeleri Getirme Fonksiyonu
  const fetchProjects = async () => {
    setLoading(true)
    try {
      const data = await projectService.getProjects()
      setRawProjects(Array.isArray(data) ? data : [])
      setError(null)
    } catch (error) {
      console.error('Projeler çekilirken hata oluştu:', error)
      setError('Projeler yüklenemedi. Lütfen bağlantınızı kontrol edin.')
    } finally {
      setLoading(false)
    }
  }

  async function fetchAuxData() {
    try {
      const [programData, customerData, userData] = await Promise.all([
        projectService.getPrograms(),
        projectService.getCustomers(),
        projectService.getUsers()
      ])
      const programsList = Array.isArray(programData) ? programData : []
      setPrograms(programsList)
      setCustomers(Array.isArray(customerData) ? customerData : [])
      setUsers(Array.isArray(userData) ? userData : [])

      if (!formData.programId && programsList.length > 0) {
        setFormData(prev => ({ ...prev, programId: programsList[0].programId }))
      }
    } catch (err) {
      console.error('Programlar, müşteriler veya kullanıcılar yüklenemedi:', err)
    }
  }

  useEffect(() => {
    void fetchProjects()
    void fetchAuxData()
  }, [])

  const getProjectManagerInfo = useCallback((project) => {
    const managerId = project.projectManagerUserId || project.projectManagerId || project.ProjectManagerUserId || ''
    const directName = project.projectManagerFullName || project.ProjectManagerFullName || project.projectManagerName || project.ProjectManagerName || ''

    if (directName) {
      return { name: directName, id: managerId || '-' }
    }

    const managerUser = users.find((user) => {
      const candidateId = user.userId || user.UserId || user.id || user.Id
      return candidateId && candidateId === managerId
    })

    if (managerUser) {
      const fullName = managerUser.fullName || managerUser.FullName || managerUser.userName || managerUser.UserName || '-'
      return { name: fullName, id: managerId || managerUser.userId || managerUser.UserId || '-' }
    }

    return {
      name: '-',
      id: managerId || '-'
    }
  }, [users])

  // 🔍 Filtreleme ve Sıralama
  const filteredProjects = useMemo(() => {
    return rawProjects
      .filter((project) => {
        const pName = project.projectName || ''
        const pCode = project.projectCode || ''
        const pProgress = project.projectStatus || ''
        const pHealth = project.manualHealth || ''
        const pFinish = getProjectFinishDate(project)
        const managerInfo = getProjectManagerInfo(project)

        if (filters.name && !pName.toLowerCase().includes(filters.name.toLowerCase())) return false
        if (filters.code && !pCode.toLowerCase().includes(filters.code.toLowerCase())) return false
        if (filters.progress !== 'Hepsi' && pProgress !== filters.progress) return false
        if (filters.health !== 'Hepsi' && pHealth !== filters.health) return false
        if (filters.isActive !== 'Hepsi') {
          const isActiveFlag = Number(project.isActive ?? project.IsActive ?? 1)
          if (filters.isActive === 'Devam Ediyor' && isActiveFlag !== 1) return false
          if (filters.isActive === 'İptal Edildi' && isActiveFlag !== 0) return false
        }
        if (filters.manager && managerInfo.id !== filters.manager) return false
        if (filters.finish && pFinish !== filters.finish) return false

        return true
      })
      .sort((a, b) => {
        if (filters.budgetSort !== 'Yok') {
          const valueA = Number(a.bac ?? a.Bac ?? 0)
          const valueB = Number(b.bac ?? b.Bac ?? 0)
          if (!Number.isNaN(valueA) && !Number.isNaN(valueB) && valueA !== valueB) {
            return filters.budgetSort === 'Artan' ? valueA - valueB : valueB - valueA
          }
        }

        if (filters.finishSort !== 'Yok') {
          const dateA = getProjectFinishDate(a)
          const dateB = getProjectFinishDate(b)
          if (!dateA && dateB) return 1
          if (dateA && !dateB) return -1
          if (!dateA && !dateB) return 0
          return filters.finishSort === 'Artan' ? dateA.localeCompare(dateB) : dateB.localeCompare(dateA)
        }

        return 0
      })
  }, [rawProjects, filters, getProjectManagerInfo])

  const totalPages = Math.max(1, Math.ceil(filteredProjects.length / pageSize))
  const paginatedProjects = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize
    return filteredProjects.slice(startIndex, startIndex + pageSize)
  }, [filteredProjects, currentPage])

  // --- MODAL İŞLEMLERİ ---
  const handleOpenCreateModal = () => {
    setEditingProjectId(null)
    setFormData({
      ...initialFormState,
      programId: programs[0]?.programId || ''
    })
    setModalError('')
    setIsModalOpen(true)
  }

  const handleOpenEditModal = async (e, project) => {
    e.stopPropagation() 
    setEditingProjectId(project.projectId)
    setModalError('')

    try {
      let detail = project
      if (projectService.getProjectById) {
        detail = await projectService.getProjectById(project.projectId)
      }

      setFormData({
        projectCode: detail.projectCode || '',
        projectName: detail.projectName || '',
        projectDescription: detail.projectDescription || '',
        projectStatus: detail.projectStatus || 'Taslak',
        manualHealth: detail.manualHealth || 'Yeşil',
        plannedProgress: detail.plannedProgress || 0,
        actualProgress: detail.actualProgress || 0,
        bac: detail.bac || 0,
        currency: detail.currency || 'TRY',
        startDate: detail.startDate ? detail.startDate.split('T')[0] : '',
        baselineFinishDate: detail.baselineFinishDate ? detail.baselineFinishDate.split('T')[0] : '',
        forecastFinishDate: detail.forecastFinishDate ? detail.forecastFinishDate.split('T')[0] : '',
        programId: detail.programId || 'PRG-001',
        customerId: detail.customerId || 'CST-001',
        projectManagerUserId: detail.projectManagerUserId || '',
        confidentiality: detail.confidentiality || 'Şirket İçi',
        isActive: detail.isActive !== undefined ? Number(detail.isActive) : 1
      })
      setIsModalOpen(true)
    } catch (error) {
      console.error('Proje detayları alınırken bir hata oluştu:', error)
      addAlert('Proje detayları alınırken bir hata oluştu.', 'error')
    }
  }

  const handleModalClose = () => {
    setIsModalOpen(false)
    setEditingProjectId(null)
    setFormData(initialFormState)
  }

  const handleFormChange = (key, value) => {
    setFormData(prev => ({ ...prev, [key]: value }))
  }

  // --- FORM GÖNDERİMİ ---
  const handleFormSubmit = async (e) => {
    e.preventDefault()
    setFormSubmitting(true)
    setModalError('')

    try {
      if (editingProjectId) {
        const updatePayload = {
          projectName: formData.projectName,
          projectDescription: formData.projectDescription,
          projectStatus: formData.projectStatus,
          manualHealth: formData.manualHealth,
          plannedProgress: Number(formData.plannedProgress),
          actualProgress: Number(formData.actualProgress),
          bac: Number(formData.bac),
          currency: formData.currency,
          forecastFinishDate: formData.forecastFinishDate ? new Date(formData.forecastFinishDate).toISOString() : null,
          confidentiality: formData.confidentiality,
          projectManagerUserId: formData.projectManagerUserId || null,
          isActive: Number(formData.isActive)
        }
        await projectService.updateProject(editingProjectId, updatePayload)
      } else {
        const createPayload = {
          ...formData,
          plannedProgress: Number(formData.plannedProgress),
          actualProgress: Number(formData.actualProgress),
          bac: Number(formData.bac),
          isActive: Number(formData.isActive ?? 1),
          startDate: new Date(formData.startDate).toISOString(),
          baselineFinishDate: new Date(formData.baselineFinishDate).toISOString(),
          forecastFinishDate: new Date(formData.forecastFinishDate || formData.baselineFinishDate).toISOString()
        }

        if (!createPayload.programId || !createPayload.customerId || !createPayload.projectManagerUserId) {
          throw new Error('Program, müşteri ve proje yöneticisi ID alanları zorunludur.')
        }
        await projectService.createProject(createPayload)
      }

      handleModalClose()
      fetchProjects()
      addAlert(editingProjectId ? 'Proje başarıyla güncellendi.' : 'Yeni proje başarıyla eklendi.', 'success')
    } catch (error) {
      const apiErrorMessage = error?.response?.data?.message || error?.message || 'İşlem sırasında bir hata oluştu.'
      setModalError(apiErrorMessage)
    } finally {
      setFormSubmitting(false)
    }
  }

  // 📂 EXCEL İÇE AKTARMA İŞLEMİ (C# Backend'e Direkt Dosya Gönderimi)
  const handleExcelImport = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    setIsImporting(true)
    try {
      // 1. Dosyayı FormData içerisine ekliyoruz (C# parametre adı 'file' ile aynı olmalı)
      const formData = new FormData()
      formData.append('file', file)

      // 2. Servis üzerinden backend'e gönderiyoruz
      const response = await projectService.importProjectsExcel(formData)
      const resData = response.data || response

      // C# tarafının döndüğü JSON verileri
      const isSuccess = resData.success ?? resData.Success
      const importedCount = resData.totalImported ?? resData.TotalImported ?? 0
      const failedCount = resData.totalFailed ?? resData.TotalFailed ?? 0
      const errorList = resData.errors ?? resData.Errors ?? []

      if (isSuccess && failedCount === 0) {
        addAlert(`Excel içe aktarma başarıyla tamamlandı. Eklenen proje sayısı: ${importedCount}`, 'success')
      } else {
        const errorDetails = errorList.length > 0 ? ` Hata detayları: ${errorList.join(' • ')}` : ''
        addAlert(`İşlem tamamlandı. Başarılı: ${importedCount}, Hatalı/Atlanan: ${failedCount}${errorDetails}`, 'info')
      }

      // 3. Tabloları/Listeleri güncelliyoruz
      if (typeof fetchProjects === 'function') fetchProjects()

    } catch (error) {
      console.error('Excel yüklenirken hata oluştu:', error)
      const message = error.response?.data?.message || 'Excel dosyası yüklenirken sunucuda bir hata oluştu.'
      addAlert(message, 'error')
    } finally {
      setIsImporting(false)
      // Input'u sıfırla ki aynı dosya tekrar seçildiğinde change eventi tetiklensin
      e.target.value = '' 
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  const handleFilterChange = (key, value) => setDraftFilters(c => ({ ...c, [key]: value }))
  const applyFilters = (e) => {
    e.preventDefault()
    setFilters(draftFilters)
    setCurrentPage(1)
  }
  const resetFilters = () => {
    const empty = { name: '', code: '', progress: 'Hepsi', health: 'Hepsi', isActive: 'Hepsi', manager: '', budgetSort: 'Yok', finish: '', finishSort: 'Yok' }
    setDraftFilters(empty)
    setFilters(empty)
    setCurrentPage(1)
  }

  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  const handleRowClick = (project) => {
    const targetId = project.projectId || project.id || project.projectCode
    if (targetId) navigate(`/projects/${targetId}`)
  }

  return (
    <div className="dashboard-card full-width">
      {/* 🚀 ÜST BAŞLIK */}
      <div className="projects-header-bar">
        <h2>Projeler</h2>
        <div style={{ display: 'flex', gap: '10px' }}>
          {/* GİZLİ DOSYA INPUTU */}
          <input 
            type="file" 
            accept=".xlsx, .xls" 
            ref={fileInputRef} 
            style={{ display: 'none' }} 
            onChange={handleExcelImport} 
          />
          {canImport && (
            <button 
              className="import-project-btn" 
              onClick={() => fileInputRef.current?.click()} 
              disabled={isImporting}
              style={{ backgroundColor: '#10b981', color: 'white', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 'bold' }}
            >
              {isImporting ? '⏳ Aktarılıyor...' : '📂 Excel İçe Aktar'}
            </button>
          )}
          {canCreate && (
            <button className="add-project-btn" onClick={handleOpenCreateModal}>
              + Yeni Proje Oluştur
            </button>
          )}
        </div>
      </div>

      {/* 🔍 FİLTRE FORMU */}
      <form className="projects-filter-bar" onSubmit={applyFilters}>
        <div className="filter-item">
          <label>Proje Adı</label>
          <input
            type="text"
            value={draftFilters.name}
            onChange={(e) => handleFilterChange('name', e.target.value)}
            placeholder="Proje adı"
          />
        </div>
        <div className="filter-item">
          <label>Proje Kodu</label>
          <input
            type="text"
            value={draftFilters.code}
            onChange={(e) => handleFilterChange('code', e.target.value)}
            placeholder="Proje kodu"
          />
        </div>
        <div className="filter-item">
          <label>İlerleme</label>
          <select value={draftFilters.progress} onChange={(e) => handleFilterChange('progress', e.target.value)}>
            {progressOptions.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div className="filter-item">
          <label>Sağlık</label>
          <select value={draftFilters.health} onChange={(e) => handleFilterChange('health', e.target.value)}>
            {healthOptions.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div className="filter-item">
          <label>Aktiflik</label>
          <select value={draftFilters.isActive} onChange={(e) => handleFilterChange('isActive', e.target.value)}>
            {activeOptions.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div className="filter-item">
          <label>Yönetici</label>
          <select value={draftFilters.manager} onChange={(e) => handleFilterChange('manager', e.target.value)}>
            <option value="">Hepsi</option>
            {users.map((user) => {
              const userId = user.userId || user.UserId || user.id || user.Id
              const userName = user.fullName || user.FullName || user.userName || user.UserName || userId
              return <option key={userId} value={userId}>{userName}</option>
            })}
          </select>
        </div>
        <div className="filter-item">
          <label>Bütçe</label>
          <select value={draftFilters.budgetSort} onChange={(e) => handleFilterChange('budgetSort', e.target.value)}>
            {budgetSortOptions.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div className="filter-item">
          <label>Bitiş Tarihi</label>
          <input type="date" value={draftFilters.finish} onChange={(e) => handleFilterChange('finish', e.target.value)} />
        </div>
        <div className="filter-item">
          <label>Sıralama</label>
          <select value={draftFilters.finishSort} onChange={(e) => handleFilterChange('finishSort', e.target.value)}>
            {finishSortOptions.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>

        <div className="projects-filter-actions">
          <button type="submit" className="filter-button" disabled={loading}>
            {loading ? 'Yükleniyor...' : 'Filtrele'}
          </button>
          <button type="button" className="reset-button" onClick={resetFilters}>
            Temizle
          </button>
        </div>
      </form>

      {error && <div className="error-alert">{error}</div>}

      {/* 📋 PROJE TABLOSU */}
      <table>
        <thead>
          <tr>
            <th>Proje Adı</th>
            <th>Proje Kodu</th>
            <th>Durum</th>
            <th>Aktiflik</th>
            <th>Sağlık</th>
            <th>Yönetici</th>
            <th>Bütçe</th>
            <th>Bitiş Tarihi</th>
            <th style={{ textAlign: 'center' }}>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr><td colSpan={9} style={{ textAlign: 'center', padding: '20px' }}>Yükleniyor...</td></tr>
          ) : filteredProjects.length > 0 ? (
            paginatedProjects.map((project) => {
              const healthStyle = getHealthBadgeStyle(project.manualHealth)
              const progressStyle = getProgressBadgeStyle(project.projectStatus)
              const managerInfo = getProjectManagerInfo(project)

              return (
                <tr
                  key={project.projectId || project.projectCode}
                  onClick={() => handleRowClick(project)}
                  className="clickable-row"
                >
                  <td><strong>{project.projectName}</strong></td>
                  <td>{project.projectCode}</td>
                  <td>
                    <span style={{ backgroundColor: progressStyle.bg, color: progressStyle.text, padding: '4px 10px', borderRadius: '6px', fontWeight: 'bold' }}>
                      {project.projectStatus || '-'}
                    </span>
                  </td>
                  <td>
                    <span style={{ backgroundColor: getProjectIsActiveFlag(project) === 0 ? '#fee2e2' : '#dcfce7', color: getProjectIsActiveFlag(project) === 0 ? '#991b1b' : '#166534', padding: '4px 10px', borderRadius: '6px', fontWeight: 'bold' }}>
                      {getActiveLabel(project)}
                    </span>
                  </td>
                  <td>
                    <span style={{ backgroundColor: healthStyle.bg, color: healthStyle.text, padding: '4px 10px', borderRadius: '6px', fontWeight: 'bold' }}>
                      {project.manualHealth || '-'}
                    </span>
                  </td>
                  <td className="manager-cell">
                    <div className="manager-name">{managerInfo.name}</div>
                    <div className="manager-id">{managerInfo.id}</div>
                  </td>
                  <td>{formatCurrency(project.bac ?? project.Bac, project.currency ?? project.Currency)}</td>
                  <td>{formatDate(project.forecastFinishDate || project.baselineFinishDate)}</td>
                  
                  {/* DÜZENLEME BUTONU */}
                  <td style={{ textAlign: 'center' }}>
                    {canEditProject(project) ? (
                      <button
                        className="edit-action-btn"
                        onClick={(e) => handleOpenEditModal(e, project)}
                      >
                        ✏️ Düzenle
                      </button>
                    ) : null}
                  </td>
                </tr>
              )
            })
          ) : (
            <tr><td colSpan={9} style={{ textAlign: 'center', padding: '20px' }}>Uygun proje bulunamadı.</td></tr>
          )}
        </tbody>
      </table>

      <div className="pagination-bar">
        <span className="pagination-summary">
          Toplam {filteredProjects.length} proje • Sayfa {currentPage} / {totalPages}
        </span>
        <div className="pagination-controls">
          <button
            type="button"
            className="pagination-btn"
            onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
            disabled={currentPage === 1}
          >
            Önceki
          </button>

          {Array.from({ length: totalPages }, (_, index) => {
            const pageNumber = index + 1
            return (
              <button
                key={pageNumber}
                type="button"
                className={`pagination-page-btn ${currentPage === pageNumber ? 'active' : ''}`}
                onClick={() => setCurrentPage(pageNumber)}
              >
                {pageNumber}
              </button>
            )
          })}

          <button
            type="button"
            className="pagination-btn"
            onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
            disabled={currentPage === totalPages}
          >
            Sonraki
          </button>
        </div>
      </div>

      {/* 🪟 YENİ PROJE / DÜZENLEME MODALI */}
      {isModalOpen && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>{editingProjectId ? '✏️ Projeyi Güncelle' : '➕ Yeni Proje Oluştur'}</h3>
              <button className="modal-close-btn" onClick={handleModalClose}>✕</button>
            </div>

            {modalError && <div className="error-alert" style={{ marginBottom: '15px' }}>{modalError}</div>}

            <form onSubmit={handleFormSubmit} className="modal-form-grid">
              
              {!editingProjectId && (
                <div className="form-group">
                  <label>Proje Kodu *</label>
                  <input
                    type="text"
                    required
                    className="form-control"
                    value={formData.projectCode}
                    onChange={(e) => handleFormChange('projectCode', e.target.value)}
                    placeholder="Örn: PRJ-2026-001"
                  />
                </div>
              )}

              <div className="form-group">
                <label>Proje Adı *</label>
                <input
                  type="text"
                  required
                  className="form-control"
                  value={formData.projectName}
                  onChange={(e) => handleFormChange('projectName', e.target.value)}
                />
              </div>

              <div className="form-group full-width-field">
                <label>Açıklama</label>
                <textarea
                  className="form-control"
                  style={{ height: '70px', padding: '10px' }}
                  value={formData.projectDescription}
                  onChange={(e) => handleFormChange('projectDescription', e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Durum</label>
                <select className="form-control" value={formData.projectStatus} onChange={(e) => handleFormChange('projectStatus', e.target.value)}>
                  {statusFormOptions.map(o => <option key={o} value={o}>{o}</option>)}
                </select>
              </div>

              <div className="form-group">
                <label>Sağlık Durumu</label>
                <select className="form-control" value={formData.manualHealth} onChange={(e) => handleFormChange('manualHealth', e.target.value)}>
                  {healthFormOptions.map(o => <option key={o} value={o}>{o}</option>)}
                </select>
              </div>

              <div className="form-group">
                <label>Aktiflik Kaydı</label>
                <select 
                  className="form-control" 
                  value={formData.isActive} 
                  onChange={(e) => handleFormChange('isActive', Number(e.target.value))}
                >
                  <option value={1}>Aktif, Devam Ediyor (1)</option>
                  <option value={0}>İptal Edildi (0)</option>
                </select>
              </div>

              <div className="form-group">
                <label>Planlanan İlerleme (%)</label>
                <input
                  type="number"
                  min="0"
                  max="100"
                  className="form-control"
                  value={formData.plannedProgress}
                  onChange={(e) => handleFormChange('plannedProgress', e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Gerçekleşen İlerleme (%)</label>
                <input
                  type="number"
                  min="0"
                  max="100"
                  className="form-control"
                  value={formData.actualProgress}
                  onChange={(e) => handleFormChange('actualProgress', e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Bütçe (BAC)</label>
                <input
                  type="number"
                  className="form-control"
                  value={formData.bac}
                  onChange={(e) => handleFormChange('bac', e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Para Birimi</label>
                <select className="form-control" value={formData.currency} onChange={(e) => handleFormChange('currency', e.target.value)}>
                  {currencyOptions.map(o => <option key={o} value={o}>{o}</option>)}
                </select>
              </div>

              {!editingProjectId && (
                <>
                  <div className="form-group">
                    <label>Başlangıç Tarihi *</label>
                    <input
                      type="date"
                      required
                      className="form-control"
                      value={formData.startDate}
                      onChange={(e) => handleFormChange('startDate', e.target.value)}
                    />
                  </div>

                  <div className="form-group">
                    <label>Planlanan Bitiş Tarihi *</label>
                    <input
                      type="date"
                      required
                      className="form-control"
                      value={formData.baselineFinishDate}
                      onChange={(e) => handleFormChange('baselineFinishDate', e.target.value)}
                    />
                  </div>
                </>
              )}

              <div className="form-group">
                <label>Tahmini Bitiş Tarihi</label>
                <input
                  type="date"
                  className="form-control"
                  value={formData.forecastFinishDate}
                  onChange={(e) => handleFormChange('forecastFinishDate', e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Müşteri *</label>
                <select
                  className="form-control"
                  required
                  value={formData.customerId}
                  onChange={(e) => handleFormChange('customerId', e.target.value)}
                >
                  <option value="">Müşteri seçin</option>
                  {customers.map((customer) => (
                    <option key={customer.customerId} value={customer.customerId}>
                      {customer.customerName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Proje Yöneticisi *</label>
                <select
                  className="form-control"
                  required
                  value={formData.projectManagerUserId}
                  onChange={(e) => handleFormChange('projectManagerUserId', e.target.value)}
                >
                  <option value="">Proje yöneticisi seçin</option>
                  {users.map((user) => (
                    <option key={user.userId} value={user.userId}>
                      {user.fullName} {user.userRole ? `(${user.userRole})` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <input type="hidden" value={formData.programId} />

              <div className="modal-actions full-width-field">
                <button type="button" className="reset-button" onClick={handleModalClose}>
                  İptal
                </button>
                <button type="submit" className="rg-submit-btn" disabled={formSubmitting}>
                  {formSubmitting ? 'Kaydediliyor...' : editingProjectId ? 'Güncelle' : 'Oluştur'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

export default ProjectsPage