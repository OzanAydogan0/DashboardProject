import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authService } from '../services/authService'
import './LoginPage.css'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const navigate = useNavigate()

  const handleSubmit = async (e) => {
    e.preventDefault()
    setIsLoading(true)

    try {
      // Artık doğrudan authService üzerinden login oluyoruz
      await authService.login(email, password)
      navigate("/") 
    } catch (error) {
      console.error("Giriş hatası:", error)
      const errorMessage = error.response?.data?.message || "Giriş başarısız! E-posta veya şifre hatalı."
      alert(errorMessage)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="login-shell">
      <aside className="login-sidebar">
        <div className="login-sidebar-overlay" />
        <div className="login-sidebar-content">
          <div className="login-sidebar-logo">
            <span className="material-symbols-outlined">vpn_key</span>
          </div>
          <div>
            <h1>PİR Dashboard</h1>
            <p>Proje İlerleme ve Takip Sistemi</p>
          </div>
        </div>
        <div className="login-sidebar-footer">
          <p>© 2026 yakisikliadnanbey bey, bilge hanım ve ozi. All rights reserved.</p>
        </div>
      </aside>

      <main className="login-main">
        <div className="login-card">
          <h1 className="login-intro">Hoş Geldiniz</h1>
          <form className="login-form" onSubmit={handleSubmit}>
            <label>
              <span>E-posta</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="e-posta"
                required
                disabled={isLoading}
              />
            </label>
            
            <label>
              <span>Şifre</span>
              <div className="password-field">
                <input
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Şifre"
                  required
                  disabled={isLoading}
                />
                <button
                  type="button"
                  className="toggle-password"
                  onClick={() => setShowPassword(!showPassword)}
                  disabled={isLoading}
                >
                  <span className="material-symbols-outlined" style={{ fontSize: '20px', verticalAlign: 'middle' }}>
                    {showPassword ? "Gizle" : "Göster"}
                  </span>
                </button>
              </div>
            </label>

            <button type="submit" className="login-button" disabled={isLoading}>
              {isLoading ? "Giriş Yapılıyor..." : "Giriş Yap"}
            </button>
          </form>
        </div>
      </main>
    </div>
  )
}

export default LoginPage