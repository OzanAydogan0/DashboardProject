import { lazy, Suspense, useEffect, useRef, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate, Outlet, useLocation, Link, useNavigate } from './router'
import './App.css'
import api from './services/api'

// İkonlar
import homeIcon from './icons/home_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import folderOpenIcon from './icons/folder_open_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import riskIcon from './icons/emergency_home_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import settingsIcon from './icons/settings_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import reportIcon from './icons/lab_profile_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'
import actionsIcon from './icons/ads_click_24dp_FFFFFF_FILL0_wght400_GRAD0_opsz24.png'

const HomePage = lazy(() => import('./pages/HomePage'))
const ProjectsPage = lazy(() => import('./pages/ProjectsPage'))
const ProjectDetailPage = lazy(() => import('./pages/ProjectDetailPage'))
const ReportsPage = lazy(() => import('./pages/ReportsPage'))
const RisksPage = lazy(() => import('./pages/RisksPage'))
const ActionsPage = lazy(() => import('./pages/ActionsPage'))
const SettingsPage = lazy(() => import('./pages/SettingsPage'))
const LoginPage = lazy(() => import('./pages/LoginPage'))

const pageInfo = {
  '/': { title: 'Portföy Dashboard'},
  '/projects': { title: 'Projeler'},
  '/reports': { title: 'Raporlar ve PİR'},
  '/risks': { title: 'Risk Yönetimi'},
  '/actions': { title: 'Aksiyon Takibi'},
  '/settings': { title: 'Sistem Ayarları'},
}

const ProtectedLayout = () => {
  const token = localStorage.getItem('token')
  const navigate = useNavigate()
  const location = useLocation()
  const [profileUser, setProfileUser] = useState(null)
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const closeSidebarTimerRef = useRef(null)

  // Kullanıcı verisini güvenli bir şekilde okuyoruz
  const userString = localStorage.getItem('user');
  let storedUser = null;
  try {
    if (userString && userString !== "undefined") {
      storedUser = JSON.parse(userString);
    }
  } catch (error) {
    console.error("Kullanıcı bilgisi okunurken hata oluştu:", error);
  }

  const clearSidebarCloseTimer = () => {
    if (closeSidebarTimerRef.current) {
      window.clearTimeout(closeSidebarTimerRef.current)
      closeSidebarTimerRef.current = null
    }
  }

  const handleSidebarMouseEnter = () => {
    clearSidebarCloseTimer()
    setIsSidebarOpen(true)
  }

  const handleSidebarMouseLeave = () => {
    clearSidebarCloseTimer()
    closeSidebarTimerRef.current = window.setTimeout(() => {
      setIsSidebarOpen(false)
    }, 0)
  }

  useEffect(() => {
    return () => clearSidebarCloseTimer()
  }, [])

  useEffect(() => {
    if (!token) return

    let isActive = true

    const loadProfile = async () => {
      try {
        const response = await api.get('/auth/me')
        const payload = response.data || {}
        const nextUser = {
          userId: payload.userId || payload.UserId,
          fullName: payload.fullName || payload.FullName,
          userRole: payload.role || payload.userRole || payload.Role || payload.UserRole,
        }

        if (isActive) {
          setProfileUser(nextUser)
          localStorage.setItem('user', JSON.stringify(nextUser))
        }
      } catch (error) {
        console.error('Kullanıcı profili alınamadı:', error)
      }
    }

    loadProfile()

    return () => {
      isActive = false
    }
  }, [token])

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

  // DİNAMİK ROL VE İSİM OKUMA
  const currentUser = profileUser || storedUser
  const userName = currentUser?.fullName || currentUser?.FullName || currentUser?.name || currentUser?.Name || 'Kullanıcı';
  const userRole = currentUser?.userRole || currentUser?.UserRole || currentUser?.role || currentUser?.Role || 'Rol Tanımsız';

  return (
    <div className={`app-shell ${isSidebarOpen ? 'sidebar-open' : ''}`}>
      <aside
        className={`sidebar ${isSidebarOpen ? 'is-open' : ''}`}
        onMouseEnter={handleSidebarMouseEnter}
        onMouseLeave={handleSidebarMouseLeave}
      >
        <div className="sidebar-brand">PİR Dashboard</div>
        <nav className="sidebar-nav">
          <Link to="/" className={`sidebar-nav-link${isActiveLink('/') ? ' active' : ''}`}>
            <img src={homeIcon} alt="Ana Sayfa" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Ana Sayfa</span>
          </Link>
          <Link to="/projects" className={`sidebar-nav-link${isActiveLink('/projects') ? ' active' : ''}`}>
            <img src={folderOpenIcon} alt="Projeler" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Projeler</span>
          </Link>
          <Link to="/reports" className={`sidebar-nav-link${isActiveLink('/reports') ? ' active' : ''}`}>
            <img src={reportIcon} alt="Raporlar" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Raporlar</span>
          </Link>
          <Link to="/risks" className={`sidebar-nav-link${isActiveLink('/risks') ? ' active' : ''}`}>
            <img src={riskIcon} alt="Riskler" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Riskler</span>
          </Link>
          <Link to="/actions" className={`sidebar-nav-link${isActiveLink('/actions') ? ' active' : ''}`}>
            <img src={actionsIcon} alt="Aksiyonlar" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Aksiyonlar</span>
          </Link>
          <Link to="/settings" className={`sidebar-nav-link${isActiveLink('/settings') ? ' active' : ''}`}>
            <img src={settingsIcon} alt="Ayarlar" className="sidebar-link-icon" />
            <span className="sidebar-link-label">Ayarlar</span>
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
        {/* DÜZELTİLEN BAŞLIK ALANI */}
        <header className="page-header">
          <h1>{currentPage.title}</h1>
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
      <Suspense fallback={<div className="page-content">Yükleniyor...</div>}>
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
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}

export default App
