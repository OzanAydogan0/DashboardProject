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
import ProjectDetailPage from './pages/ProjectDetailPage'
import ReportsPage from './pages/ReportsPage'
import RisksPage from './pages/RisksPage'
import ActionsPage from './pages/ActionsPage'
import SettingsPage from './pages/SettingsPage'
import LoginPage from './pages/LoginPage'

const pageInfo = {
  '/': { title: 'Portföy Dashboard'},
  '/projects': { title: 'Projeler'},
  '/reports': { title: 'Raporlar ve PİR'},
  '/risks': { title: 'Risk Yönetimi'},
  '/actions': { title: 'Aksiyon Takibi'},
  '/settings': { title: 'Sistem Ayarları'},
}

// App.jsx içerisindeki ProtectedLayout bileşeni
const ProtectedLayout = () => {
  const token = localStorage.getItem('token')
  const navigate = useNavigate()
  const location = useLocation()

  // Kullanıcı verisini güvenli bir şekilde okuyoruz
  const userString = localStorage.getItem('user');
  let user = null;
  try {
    if (userString && userString !== "undefined") {
      user = JSON.parse(userString);
    }
  } catch (error) {
    console.error("Kullanıcı bilgisi okunurken hata oluştu:", error);
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    navigate('/login')
  }

  const currentPage = pageInfo[location.pathname] || { title: 'Proje Detayı', description: '' }
  const isActiveLink = (path) => path === '/' ? location.pathname === '/' : location.pathname === path || location.pathname.startsWith(`${path}/`)

  // 💡 DİNAMİK ROL VE İSİM OKUMA (PascalCase / camelCase Esnekliği)
  const userName = user?.fullName || user?.FullName || user?.name || user?.Name || 'Kullanıcı';
  const userRole = user?.userRole || user?.UserRole || user?.role || user?.Role || 'Rol Tanımsız';

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">PİR Dashboard</div>
        <nav className="sidebar-nav">
          <Link to="/" className={`sidebar-nav-link${isActiveLink('/') ? ' active' : ''}`}>
            <img src={homeIcon} alt="Ana Sayfa" className="sidebar-link-icon" />
            Ana Sayfa
          </Link>
          <Link to="/projects" className={`sidebar-nav-link${isActiveLink('/projects') ? ' active' : ''}`}>
            <img src={folderOpenIcon} alt="Projeler" className="sidebar-link-icon" />
            Projeler
          </Link>
          <Link to="/reports" className={`sidebar-nav-link${isActiveLink('/reports') ? ' active' : ''}`}>
            <img src={reportIcon} alt="Raporlar" className="sidebar-link-icon" />
            Raporlar
          </Link>
          <Link to="/risks" className={`sidebar-nav-link${isActiveLink('/risks') ? ' active' : ''}`}>
            <img src={riskIcon} alt="Riskler" className="sidebar-link-icon" />
            Riskler
          </Link>
          <Link to="/actions" className={`sidebar-nav-link${isActiveLink('/actions') ? ' active' : ''}`}>
            <img src={actionsIcon} alt="Aksiyonlar" className="sidebar-link-icon" />
            Aksiyonlar
          </Link>
          <Link to="/settings" className={`sidebar-nav-link${isActiveLink('/settings') ? ' active' : ''}`}>
            <img src={settingsIcon} alt="Ayarlar" className="sidebar-link-icon" />
            Ayarlar
          </Link>
        </nav>

        {/* DİNAMİK PROFİL KARTI */}
        <div className="sidebar-profile">
          <div className="profile-info">
            <span className="profile-name">{userName}</span>
            <span className="profile-role">{userRole}</span>
          </div>
        </div>

        <button className="logout-button" onClick={handleLogout}>
          Çıkış Yap
        </button>
      </aside>

      <main className="content-shell">
        <header style={{ paddingBottom: '20px', borderBottom: '1px solid var(--border)', marginBottom: '24px' }}>
          <h1 style={{ margin: 0, fontSize: '1.5rem', color: 'var(--text-h)' }}>{currentPage.title}</h1>
        </header>

        <section className="page-content">
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
          <Route path="/projects/:id" element={<ProjectDetailPage />} />
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