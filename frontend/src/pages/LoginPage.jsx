import { useState } from 'react'
import { useNavigate } from '../router'
import { authService } from '../services/authService'
import { useAlert } from '../components/alertContext'
import './LoginPage.css'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showForgotPassword, setShowForgotPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const navigate = useNavigate()
  const { addAlert } = useAlert()

  const handleSubmit = async (e) => {
    e.preventDefault()
    setIsLoading(true)

    try {
      await authService.login(email, password)
      addAlert('Giriş başarılı. Hoş geldiniz!', 'success')
      navigate("/") 
    } catch (error) {
      console.error("Giriş hatası:", error)
      const errorMessage = error.response?.data?.message || "Giriş başarısız! E-posta veya şifre hatalı."
      addAlert(errorMessage, 'error')
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
          <p>© 2026 PİR Dashboard. Tüm hakları saklıdır.</p>
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

            <div className="forgot-password-wrapper">
              <button
                type="button"
                className="forgot-password-link"
                onClick={() => setShowForgotPassword((prev) => !prev)}
                disabled={isLoading}
              >
                {showForgotPassword ? 'Şifremi unuttum alanını kapat' : 'Şifremi unuttum'}
              </button>

              {showForgotPassword && (
                <div className="forgot-password-panel" role="status">
                  <strong>Parola sıfırlama</strong>
                  <p>
                    Güvenlik nedeniyle parolanızı yalnızca sistem yöneticiniz
                    sıfırlayabilir. Kurumunuzun yetkili destek kanalıyla
                    iletişime geçin.
                  </p>
                </div>
              )}
            </div>
          </form>
        </div>
      </main>
    </div>
  )
}

export default LoginPage
