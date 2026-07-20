import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import './LoginPage.css'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const navigate = useNavigate()

  const handleSubmit = async (e) => {
    e.preventDefault()
    setIsLoading(true)

    try {
      const response = await fetch("http://localhost:5074/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => null)
        alert(errorData?.message || "Giriş başarısız! E-posta veya şifre hatalı.")
        setIsLoading(false)
        return
      }

      const data = await response.json()
      
      // Token'ı kaydet
      localStorage.setItem("token", data.token)
      
      // Ana sayfaya yönlendir
      navigate("/") 

    } catch (error) {
      console.error("Bağlantı hatası:", error)
      alert("API ile iletişim kurulamadı. Backend (Port: 5074) açık mı?")
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
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Şifre"
                required
                disabled={isLoading}
              />
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