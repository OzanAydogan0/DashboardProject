import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authService } from '../services/authService'
import { useAlert } from '../components/AlertProvider'
import { notifyPasswordChangeRequest } from '../utils/adminNotifications'
import './LoginPage.css'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [forgotName, setForgotName] = useState('')
  const [forgotEmail, setForgotEmail] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showForgotPassword, setShowForgotPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [isForgotLoading, setIsForgotLoading] = useState(false)
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

  const handleForgotPassword = async (e) => {
    e.preventDefault()

    if (!forgotName.trim()) {
      addAlert('Lütfen isim soyisim girin.', 'error')
      return
    }

    setIsForgotLoading(true)

    try {
      const request = notifyPasswordChangeRequest({
        fullName: forgotName.trim(),
        email: forgotEmail.trim() || email.trim(),
      })

      addAlert(`Şifre değiştirme isteğiniz yöneticilere iletildi. ${request.fullName} için işlem takip edilecek.`, 'success')
      setForgotName('')
      setForgotEmail('')
    } catch (error) {
      console.error('Şifre değiştirme isteği gönderilemedi:', error)
      addAlert('İstek gönderilirken bir hata oluştu. Lütfen tekrar deneyin.', 'error')
    } finally {
      setIsForgotLoading(false)
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

            <div className="forgot-password-wrapper">
              <button
                type="button"
                className="forgot-password-link"
                onClick={() => setShowForgotPassword((prev) => !prev)}
                disabled={isLoading || isForgotLoading}
              >
                {showForgotPassword ? 'Şifremi unuttum alanını kapat' : 'Şifremi unuttum'}
              </button>

              {showForgotPassword && (
                <div className="forgot-password-panel">
                  <label className="forgot-password-field">
                    <span>İsim Soyisim</span>
                    <input
                      type="text"
                      value={forgotName}
                      onChange={(e) => setForgotName(e.target.value)}
                      placeholder="Ad Soyad"
                      disabled={isLoading || isForgotLoading}
                    />
                  </label>

                  <label className="forgot-password-field">
                    <span>E-posta</span>
                    <input
                      type="email"
                      value={forgotEmail}
                      onChange={(e) => setForgotEmail(e.target.value)}
                      placeholder="e-posta"
                      disabled={isLoading || isForgotLoading}
                    />
                  </label>

                  <button type="button" className="forgot-password-button" onClick={handleForgotPassword} disabled={isLoading || isForgotLoading}>
                    {isForgotLoading ? 'Gönderiliyor...' : 'İsteği Gönder'}
                  </button>
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