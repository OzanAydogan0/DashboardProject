import { useMemo, useState } from 'react'
import './ProjectsPage.css'

const progressOptions = ['Hepsi', 'Devam Ediyor', 'Tamamlandı', 'Planlandı', 'Beklemede']
const healthOptions = ['Hepsi', 'İyi', 'Orta', 'Kritik']
const budgetSortOptions = ['Yok', 'Artan', 'Azalan']
const finishSortOptions = ['Yok', 'Artan', 'Azalan']
const budgetOrder = {
  'Yetersiz': 1,
  'Dengeli': 2,
  'Yeterli': 3,
  'Aşılmış': 4,
}

function ProjectsPage() {
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

  const projects = [
    {
      name: 'Proje 1',
      code: 'PRJ-001',
      progress: 'Devam Ediyor',
      health: 'İyi',
      budget: 'Dengeli',
      finish: '2025-06-30',
    },
    {
      name: 'Proje 2',
      code: 'PRJ-002',
      progress: 'Tamamlandı',
      health: 'İyi',
      budget: 'Yeterli',
      finish: '2024-12-15',
    },
    {
      name: 'Proje 3',
      code: 'PRJ-003',
      progress: 'Planlandı',
      health: 'Orta',
      budget: 'Yetersiz',
      finish: '2025-03-10',
    },
  ]

  const filteredProjects = useMemo(() => {
    return projects.filter((project) => {
      if (filters.name && !project.name.toLowerCase().includes(filters.name.toLowerCase())) {
        return false
      }
      if (filters.code && !project.code.toLowerCase().includes(filters.code.toLowerCase())) {
        return false
      }
      if (filters.progress !== 'Hepsi' && project.progress !== filters.progress) {
        return false
      }
      if (filters.health !== 'Hepsi' && project.health !== filters.health) {
        return false
      }
      if (filters.finish && project.finish !== filters.finish) {
        return false
      }
      return true
    }).sort((a, b) => {
      if (filters.budgetSort !== 'Yok') {
        const orderA = budgetOrder[a.budget] ?? 0
        const orderB = budgetOrder[b.budget] ?? 0
        return filters.budgetSort === 'Artan' ? orderA - orderB : orderB - orderA
      }
      if (filters.finishSort !== 'Yok') {
        return filters.finishSort === 'Artan'
          ? a.finish.localeCompare(b.finish)
          : b.finish.localeCompare(a.finish)
      }
      return 0
    })
  }, [filters])

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

  return (
    <div className="dashboard-card full-width">
      <h2>Projeler Listesi</h2>
      <p>Projelerinizi buradan takip edin ve filtreleyin.</p>
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
          <button type="submit" className="filter-button">
            Filtrele
          </button>
          <button type="button" className="reset-button" onClick={resetFilters}>
            Temizle
          </button>
        </div>
      </form>
      <table>
        <thead>
          <tr>
            <th>Proje Adı</th>
            <th>Proje Kodu</th>
            <th>İlerleme</th>
            <th>Sağlık</th>
            <th>Bütçe</th>
            <th>Bitiş Tarihi</th>
          </tr>
        </thead>
        <tbody>
          {filteredProjects.length > 0 ? (
            filteredProjects.map((project) => (
              <tr key={`${project.code}-${project.name}`}>
                <td>{project.name}</td>
                <td>{project.code}</td>
                <td>{project.progress}</td>
                <td>{project.health}</td>
                <td>{project.budget}</td>
                <td>{project.finish}</td>
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={6} style={{ textAlign: 'center' }}>
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
