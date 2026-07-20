import './HomePage.css'

function HomePage() {
  return (
    <>
      <div className="dashboard-card">
        <h2>Genel Durum</h2>
        <p>Projelerin %78 tamamlandı.</p>
      </div>
      <div className="dashboard-card">
        <h2>Riskler</h2>
        <p>Şu anda 3 aktif risk var.</p>
      </div>
      <div className="dashboard-card">
        <h2>Görevler</h2>
        <p>Bugün tamamlanması gereken 5 işin var.</p>
      </div>
    </>
  )
}

export default HomePage
