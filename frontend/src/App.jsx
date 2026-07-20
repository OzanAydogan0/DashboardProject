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

// URL yollarına göre başlıkları belirlediğimiz sözlük
const pageInfo = {
  '/': { title: 'Ana Sayfa', description: '' },
  '/projects': { title: 'Projeler', description: '' },
  '/reports': { title: 'Raporlar', description: '' },
  '/risks': { title: 'Riskler', description: '' },
  '/actions': { title: 'Aksiyonlar', description: '' },
  '/settings': { title: 'Ayarlar', description: '' },
}

// 🛡️ KORUMALI ŞABLON (LAYOUT): Sadece giriş yapanların görebileceği Dashboard iskeleti
const ProtectedLayout = () => {
  const token = localStorage.getItem('token')


  const navigate = useNavigate()
  const location = useLocation()

  // login disable
  /*if (!token) {
    return <Navigate to="/login" replace />
  }*/

  const handleLogout = () => {
    localStorage.removeItem('token')
    navigate('/login')
  }

  // Mevcut URL'ye göre sayfa başlığını bul
  const currentPage = pageInfo[location.pathname] || pageInfo['/']

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
            {/* Alt rotalar (Sayfalar) buraya render edilecek */}
            <Outlet />
          </section>
        </section>
      </main>
    </div>
  )
}

// 🚦 UYGULAMA TRAFİK POLİSİ (ANA YÖNLENDİRİCİ)
function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* HERKESE AÇIK */}
        <Route path="/login" element={<LoginPage />} />

        {/* KORUMALI DASHBOARD ROTALARI */}
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
