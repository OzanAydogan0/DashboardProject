import { useMemo, useState } from 'react'
import './ProjectsPage.css'

function ProjectsPage() {
  const [search, setSearch] = useState('')

  const projects = [
    {
      name: 'Proje 1',
      code: 'Açıklama 1',
      progress: 'Devam Ediyor',
      health: 'Devam Ediyor',
      budget: 'Devam Ediyor',
      finish: 'Devam Ediyor',
    },
    {
      name: 'Proje 2',
      code: 'Açıklama 2',
      progress: 'Tamamlandı',
      health: 'Devam Ediyor',
      budget: 'Devam Ediyor',
      finish: 'Devam Ediyor',
    },
  ]

  const filteredProjects = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return projects

    return projects.filter(({ name, code, progress, health, budget, finish }) =>
      [name, code, progress, health, budget, finish].some((value) =>
        value.toLowerCase().includes(term)
      )
    )
  }, [search])

  return (
    <div className="dashboard-card full-width">
      <h2>Projeler Listesi</h2>
      <p>Projelerinizi buradan takip edin ve güncelleyin.</p>
      <div className="projects-search">
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Proje adı, kod, durum veya bütçe ara..."
          aria-label="Proje ara"
        />
      </div>
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
            <tr key={project.code + project.name}>
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
              Arama sonuçlarınız bulunamadı.
            </td>
          </tr>
        )}
      </tbody>
      </table>
    </div>
  )
}

export default ProjectsPage
