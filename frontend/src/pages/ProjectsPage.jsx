import { useMemo, useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { projectService } from '../services/projectService'
import './ProjectsPage.css'

const progressOptions = ['Hepsi', 'Devam Ediyor', 'Tamamlandı', 'Planlandı', 'Beklemede']
const healthOptions = ['Hepsi', 'İyi', 'Orta', 'Kritik', 'Yeşil', 'Sarı', 'Kırmızı']
const budgetSortOptions = ['Yok', 'Artan', 'Azalan']
const finishSortOptions = ['Yok', 'Artan', 'Azalan']

const budgetOrder = {
  'Yetersiz': 1,
  'Dengeli': 2,
  'Yeterli': 3,
  'Aşılmış': 4,
}

function ProjectsPage() {
  const navigate = useNavigate()
  
  const [rawProjects, setRawProjects] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [draftFilters, setDraftFilters] = useState({
    name: '',
    code: '',
    progress: 'Hepsi',
    health: 'Hepsi',
    budgetSort: 'Yok',
    finish: '',
    finishSort: 'Yok',
  })
  const [filters, setFilters] = useState(draftFilters)

  useEffect(() => {
    let isMounted = true
    const fetchProjects = async () => {
      try {
        const data = await projectService.getProjects()
        if (isMounted) setRawProjects(Array.isArray(data) ? data : [])
      } catch (err) {
        console.error('Projeler çekilirken hata oluştu:', err)
        if (isMounted) setError('Projeler yüklenemedi. Lütfen bağlantınızı kontrol edin.')
      } finally {
        if (isMounted) setLoading(false)
      }
    }
    fetchProjects()
    return () => { isMounted = false }
  }, [])

  const filteredProjects = useMemo(() => {
    return rawProjects.filter((project) => {
      const pName = project.projectName || ''
      const pCode = project.projectCode || ''
      const pProgress = project.projectStatus || ''
      const pHealth = project.manualHealth || ''
      const pFinish = project.forecastFinishDate ? project.forecastFinishDate.split('T')[0] : ''

      if (filters.name && !pName.toLowerCase().includes(filters.name.toLowerCase())) return false
      if (filters.code && !pCode.toLowerCase().includes(filters.code.toLowerCase())) return false
      if (filters.progress !== 'Hepsi' && pProgress !== filters.progress) return false
      if (filters.health !== 'Hepsi' && pHealth !== filters.health) return false
      if (filters.finish && pFinish !== filters.finish) return false
      return true
    }).sort((a, b) => {
      if (filters.budgetSort !== 'Yok') {
        const orderA = budgetOrder[a.budgetStatus] ?? 0
        const orderB = budgetOrder[b.budgetStatus] ?? 0
        return filters.budgetSort === 'Artan' ? orderA - orderB : orderB - orderA
      }
      if (filters.finishSort !== 'Yok') {
        const dateA = a.forecastFinishDate || ''
        const dateB = b.forecastFinishDate || ''
        return filters.finishSort === 'Artan'
          ? dateA.localeCompare(dateB)
          : dateB.localeCompare(dateA)
      }
      return 0
    })
  }, [rawProjects, filters])

  const handleFilterChange = (key, value) => {
    setDraftFilters((current) => ({ ...current, [key]: value }))
  }

  const applyFilters = (event) => {
    event.preventDefault()
    setFilters(draftFilters)
  }

  const resetFilters = () => {
    const empty = {
      name: '',
      code: '',
      progress: 'Hepsi',
      health: 'Hepsi',
      budgetSort: 'Yok',
      finish: '',
      finishSort: 'Yok',
    }
    setDraftFilters(empty)
    setFilters(empty)
  }

  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  // Tıklama Yöneticisi
const handleRowClick = (project) => {
    // Proje ismini kesinlikle dahil etmiyoruz, sadece ID / Kodu alıyoruz
    const targetId = project.id || project.projectId || project.projectCode

    if (targetId) {
      navigate(`/projects/${targetId}`)
    } else {
      console.error('Projenin geçerli bir ID veya Proje Kodu bulunamadı:', project)
    }
  }
  return (
    <div className="dashboard-card full-width">
      {/* 🔍 EKSİKSİZ FİLTRE FORMU */}
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
          <select
            value={draftFilters.progress}
            onChange={(e) => handleFilterChange('progress', e.target.value)}
          >
            {progressOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </div>
        <div className="filter-item">
          <label>Sağlık</label>
          <select
            value={draftFilters.health}
            onChange={(e) => handleFilterChange('health', e.target.value)}
          >
            {healthOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </div>
        <div className="filter-item">
          <label>Bütçe</label>
          <select
            value={draftFilters.budgetSort}
            onChange={(e) => handleFilterChange('budgetSort', e.target.value)}
          >
            {budgetSortOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </div>
        <div className="filter-item">
          <label>Bitiş Tarihi</label>
          <input
            type="date"
            value={draftFilters.finish}
            onChange={(e) => handleFilterChange('finish', e.target.value)}
          />
        </div>
        <div className="filter-item">
          <label>Bitiş Tarihi Sıralama</label>
          <select
            value={draftFilters.finishSort}
            onChange={(e) => handleFilterChange('finishSort', e.target.value)}
          >
            {finishSortOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
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

      {error && (
        <div style={{ color: '#ef4444', padding: '12px', background: '#fee2e2', borderRadius: '14px', marginTop: '20px' }}>
          {error}
        </div>
      )}

      {/* 📋 PROJE TABLOSU */}
      <table>
        <thead>
          <tr>
            <th>Proje Adı</th>
            <th>Proje Kodu</th>
            <th>Durum (İlerleme)</th>
            <th>Sağlık</th>
            <th>Bütçe</th>
            <th>Bitiş Tarihi</th>
          </tr>
        </thead>
<tbody>
  {loading ? (
    <tr>
      <td colSpan={6} style={{ textAlign: 'center', padding: '20px' }}>
        Projeler yükleniyor...
      </td>
    </tr>
  ) : filteredProjects.length > 0 ? (
    filteredProjects.map((project) => (
      <tr 
        key={project.projectId || project.projectCode} 
        // 👇 DİKKAT: Sadece (project) gönderiyoruz ki yukarıdaki fonksiyonunuz çalışsın
        onClick={() => handleRowClick(project)} 
        className="clickable-row"
      >
        <td>{project.projectName}</td>
        <td>{project.projectCode}</td>
        <td>{project.projectStatus || '-'}</td>
        <td>{project.manualHealth || '-'}</td>
        <td>{project.budgetStatus || '-'}</td>
        <td>{formatDate(project.forecastFinishDate || project.baselineFinishDate)}</td>
      </tr>
    ))
  ) : (
    <tr>
      <td colSpan={6} style={{ textAlign: 'center', padding: '20px' }}>
        Filtrelerinize uygun proje bulunamadı.
      </td>
    </tr>
  )}
</tbody>
      </table>
    </div>
  )
}

export default ProjectsPage