import { BrowserRouter, Routes, Route, Navigate, Outlet, useLocation, Link, useNavigate } from 'react-router-dom'
import './App.css'

// İkonlar
import homeIcon from './icons/home_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import folderOpenIcon from './icons/folder_open_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import riskIcon from './icons/emergency_home_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import settingsIcon from './icons/settings_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import reportIcon from './icons/lab_profile_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import actionsIcon from './icons/ads_click_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'

// Sayfalar
import HomePage from './pages/HomePage'
import ProjectsPage from './pages/ProjectsPage'
import ReportsPage from './pages/ReportsPage'
import RisksPage from './pages/RisksPage'
import ActionsPage from './pages/ActionsPage'
import SettingsPage from './pages/SettingsPage'
import LoginPage from './pages/LoginPage'

const pageInfo = {
  '/': { title: 'Portföy Dashboard', description: 'Tüm projelerin genel durum özeti ve KPI değerleri' },
  '/projects': { title: 'Projeler', description: 'Aktif ve geçmiş proje listesi' },
  '/reports': { title: 'Raporlar ve PİR', description: 'Aylık PİR raporları ve PDF çıktıları' },
  '/risks': { title: 'Risk Yönetimi', description: 'Tüm projelerdeki aktif ve kritik riskler' },
  '/actions': { title: 'Aksiyon Takibi', description: 'Açık ve geciken aksiyon kayıtları' },
  '/settings': { title: 'Sistem Ayarları', description: 'Kullanıcı yönetimi ve parametre eşikleri' },
}

const ProtectedLayout = () => {
  const token = localStorage.getItem('token')
  const navigate = useNavigate()
  const location = useLocation()

  if (!token) {
    return <Navigate to="/login" replace />
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    navigate('/login')
  }

  // Dinamik rotalar (/projects/PRJ-001 gibi) için varsayılan başlık
  const currentPage = pageInfo[location.pathname] || { title: 'Proje Detayı', description: '' }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">PİR Dashboard</div>
        <nav className="sidebar-nav">
          <Link to="/" className="sidebar-nav-link">
            <img src={homeIcon} alt="Ana Sayfa" className="sidebar-link-icon" />
            Ana Sayfa
          </Link>
          <Link to="/projects" className="sidebar-nav-link">
            <img src={folderOpenIcon} alt="Projeler" className="sidebar-link-icon" />
            Projeler
          </Link>
          <Link to="/reports" className="sidebar-nav-link">
            <img src={reportIcon} alt="Raporlar" className="sidebar-link-icon" />
            Raporlar
          </Link>
          <Link to="/risks" className="sidebar-nav-link">
            <img src={riskIcon} alt="Riskler" className="sidebar-link-icon" />
            Riskler
          </Link>
          <Link to="/actions" className="sidebar-nav-link">
            <img src={actionsIcon} alt="Aksiyonlar" className="sidebar-link-icon" />
            Aksiyonlar
          </Link>
          <Link to="/settings" className="sidebar-nav-link">
            <img src={settingsIcon} alt="Ayarlar" className="sidebar-link-icon" />
            Ayarlar
          </Link>
        </nav>
        <button className="logout-button" onClick={handleLogout}>
          Çıkış Yap
        </button>
      </aside>

      <main className="content-shell">
        <section className="page-content">
          <header className="dashboard-header dashboard-top">
            <div>
              <h1>{currentPage.title}</h1>
              <p>{currentPage.description}</p>
            </div>
          </header>

          <section className="dashboard-grid">
            <Outlet />
          </section>
        </section>
      </main>
    </div>
  )
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<ProtectedLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/risks" element={<RisksPage />} />
          <Route path="/actions" element={<ActionsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App