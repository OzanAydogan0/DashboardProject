import './HomePage.css'

function HomePage() {
  return (
    <div className="page-content">
      {/* 🟢 1. SATIR: 5 Adet Küçük Kare Kart */}
      <div className="top-grid">
        <div className="small-card">
          <h3>Toplam Proje</h3>
          <p className="stat-number">12</p>
        </div>
        <div className="small-card">
          <h3>Riskli Projeler</h3>
          <p className="stat-number">32</p>
        </div>
        <div className="small-card">
          <h3>Geciken Projeler</h3>
          <p className="stat-number">43</p>
        </div>
        <div className="small-card">
          <h3>Tamamlanan</h3>
          <p className="stat-number">88</p>
        </div>
        <div className="small-card">
          <h3>Aktif Projeler</h3>
          <p className="stat-number">15</p>
        </div>
      </div>

      {/* 🔵 2. SATIR: 2 Adet Orta Boy Kart */}
      <div className="medium-grid">
        <div className="dashboard-card">
          <h2>Proje Durumu</h2>
          <p>Bugün tamamlanması gereken 5 işin var.</p>
        </div>
        <div className="dashboard-card">
          <h2>Proje Sağlık Durumu</h2>
          <p>Bugün tamamlanması gereken 5 işin var.</p>
        </div>
      </div>

      {/* 🟣 3. SATIR: 2 Adet Orta Boy Kart */}
      <div className="medium-grid">
        <div className="dashboard-card">
          <h2>Risk Detayları</h2>
          <p>Bugün tamamlanması gereken 5 işin var.</p>
        </div>
        <div className="dashboard-card">
          <h2>Performans Özeti</h2>
          <p>Bugün tamamlanması gereken 5 işin var.</p>
        </div>
      </div>
    </div>
  )
}

export default HomePage