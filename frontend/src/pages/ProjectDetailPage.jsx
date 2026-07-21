import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { projectService } from '../services/projectService'
import './ProjectDetailPage.css'

function ProjectDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  // Aktif Sekme State'i
  const [activeTab, setActiveTab] = useState('summary') 
  
  const [projectData, setProjectData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    let isMounted = true

    const fetchProjectDetails = async () => {
      try {
        setLoading(true)
        
        // ⚡ Backend'deki iki endpoint'ten (Dashboard Özeti ve EVM Geçmişi) verileri eşzamanlı çekiyoruz
        const [dashboardData, evmData] = await Promise.all([
          projectService.getProjectDashboard(id),
          projectService.getProjectEvm(id).catch((err) => {
            console.warn('EVM verisi çekilemedi veya boş:', err)
            return null // EVM API'si hata verirse sayfanın tamamı çökmesin
          })
        ])

        if (isMounted) {
          // İki veriyi tek bir state objesinde birleştiriyoruz
          setProjectData({
            ...dashboardData,
            evmDetails: evmData || dashboardData.evm || {} 
          })
        }
      } catch (err) {
        console.error('Proje detayları alınamadı:', err)
        if (isMounted) {
          setError('Proje detayları veritabanından çekilemedi.')
        }
      } finally {
        if (isMounted) {
          setLoading(false)
        }
      }
    }

    if (id) {
      fetchProjectDetails()
    }

    return () => { isMounted = false }
  }, [id])

  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  if (loading) {
    return <div className="page-content"><div className="dashboard-card"><p>Proje detayları yükleniyor...</p></div></div>
  }

  if (error || !projectData) {
    return (
      <div className="page-content">
        <div className="dashboard-card">
          <p style={{ color: '#ef4444' }}>{error || 'Proje bulunamadı.'}</p>
          <button className="reset-button" onClick={() => navigate('/projects')}>← Projelere Dön</button>
        </div>
      </div>
    )
  }

  // Güvenli veri erişimi için varsayılanlar
  const milestones = projectData.milestones || []
  const risks = projectData.risks || []
  const issues = projectData.issues || []
  const actions = projectData.actions || []
  const pirReports = projectData.reports || []
  
  // EVM verisi nesne mi yoksa dizi mi (Geçmiş listesi) olarak geliyor kontrolü
  const evm = Array.isArray(projectData.evmDetails) 
    ? projectData.evmDetails[0] || {} // Dizi ise en güncel (ilk) kaydı al
    : projectData.evmDetails || {}

  return (
    <div className="page-content project-detail-container">
      {/* 🔴 ÜST BAŞLIK ALANI */}
      <div className="dashboard-header project-detail-header">
        <div className="header-top-row">
          <button className="back-btn" onClick={() => navigate('/projects')}>
            ← Projelere Dön
          </button>
          <span className={`badge-health badge-${(projectData.manualHealth || 'yesil').toLowerCase()}`}>
            ● Sağlık: {projectData.manualHealth || 'Yeşil'}
          </span>
        </div>

        <div className="project-title-section">
          <h1>{projectData.projectName || 'Proje Detayı'}</h1>
          <div className="project-meta-pills">
            <span><strong>Kod:</strong> {projectData.projectCode}</span>
            <span><strong>Müşteri:</strong> {projectData.customerName || '-'}</span>
            <span><strong>Yönetici:</strong> {projectData.projectManager || '-'}</span>
            <span><strong>Durum:</strong> {projectData.projectStatus || 'Aktif'}</span>
          </div>
        </div>

        {/* 📌 İSTENEN 7 SEKMELİ NAVBAR */}
        <div className="project-nav-tabs">
          <button className={`tab-item ${activeTab === 'summary' ? 'active' : ''}`} onClick={() => setActiveTab('summary')}>
            Genel Özet
          </button>
          <button className={`tab-item ${activeTab === 'milestones' ? 'active' : ''}`} onClick={() => setActiveTab('milestones')}>
            Kilometre Taşları ({milestones.length})
          </button>
          <button className={`tab-item ${activeTab === 'risks' ? 'active' : ''}`} onClick={() => setActiveTab('risks')}>
            Riskler ({risks.length})
          </button>
          <button className={`tab-item ${activeTab === 'issues' ? 'active' : ''}`} onClick={() => setActiveTab('issues')}>
            Sorunlar ({issues.length})
          </button>
          <button className={`tab-item ${activeTab === 'actions' ? 'active' : ''}`} onClick={() => setActiveTab('actions')}>
            Aksiyonlar ({actions.length})
          </button>
          <button className={`tab-item ${activeTab === 'evm' ? 'active' : ''}`} onClick={() => setActiveTab('evm')}>
            EVM
          </button>
          <button className={`tab-item ${activeTab === 'reports' ? 'active' : ''}`} onClick={() => setActiveTab('reports')}>
            PİR Dönemleri ({pirReports.length})
          </button>
        </div>
      </div>

      {/* 📊 1. SEKME: GENEL ÖZET */}
      {activeTab === 'summary' && (
        <div className="tab-content-wrapper">
          <div className="dashboard-grid">
            <div className="dashboard-card">
              <h3>Planlanan İlerleme</h3>
              <p className="kpi-large-num">%{projectData.plannedProgress || 0}</p>
            </div>
            <div className="dashboard-card">
              <h3>Gerçekleşen İlerleme</h3>
              <p className="kpi-large-num">%{projectData.actualProgress || 0}</p>
            </div>
            <div className="dashboard-card">
              <h3>SPI (Zaman Performansı)</h3>
              <p className="kpi-large-num">{evm.spi ?? '-'}</p>
            </div>
            <div className="dashboard-card">
              <h3>CPI (Maliyet Performansı)</h3>
              <p className="kpi-large-num">{evm.cpi ?? '-'}</p>
            </div>
          </div>

          <div className="dashboard-card full-width" style={{ marginTop: '24px' }}>
            <h2>Yönetici Özeti</h2>
            <p className="summary-text-box">
              {projectData.executiveSummary || 'Bu proje için henüz yayımlanmış son bir PİR yönetici özeti bulunmuyor.'}
            </p>
          </div>

          <div className="dashboard-grid" style={{ marginTop: '24px' }}>
            <div className="dashboard-card">
              <h2>Tarih Bilgileri</h2>
              <p><strong>Başlangıç:</strong> {formatDate(projectData.startDate)}</p>
              <p><strong>Temel Bitiş (Baseline):</strong> {formatDate(projectData.baselineFinishDate)}</p>
              <p><strong>Tahmini Bitiş (Forecast):</strong> {formatDate(projectData.forecastFinishDate)}</p>
            </div>
            <div className="dashboard-card">
              <h2>Açık Kayıt Sayıları</h2>
              <p><strong>Açık Risk Sayısı:</strong> {projectData.openRiskCount || 0}</p>
              <p><strong>Açık Sorun Sayısı:</strong> {projectData.openIssueCount || 0}</p>
              <p><strong>Açık Aksiyon Sayısı:</strong> {projectData.openActionCount || 0}</p>
            </div>
          </div>
        </div>
      )}

      {/* 🏁 2. SEKME: KİLOMETRE TAŞLARI */}
      {activeTab === 'milestones' && (
        <div className="dashboard-card full-width">
          <h2>Kilometre Taşları</h2>
          <table>
            <thead>
              <tr>
                <th>Adı</th>
                <th>Sorumlu</th>
                <th>Planlanan Tarih</th>
                <th>Tahmini Tarih</th>
                <th>Gerçekleşen Tarih</th>
                <th>Kritik</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {milestones.length > 0 ? (
                milestones.map((m, idx) => (
                  <tr key={m.id || idx}>
                    <td className="fw-medium">{m.name}</td>
                    <td>{m.owner || '-'}</td>
                    <td>{formatDate(m.plannedDate)}</td>
                    <td>{formatDate(m.forecastDate)}</td>
                    <td>{formatDate(m.actualDate)}</td>
                    <td>{m.critical ? '⚠️ Evet' : 'Hayır'}</td>
                    <td>{m.status || 'Planlandı'}</td>
                  </tr>
                ))
              ) : (
                <tr><td colSpan={7} style={{ textAlign: 'center' }}>Tanımlı kilometre taşı bulunmuyor.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* ⚠️ 3. SEKME: RİSKLER */}
      {activeTab === 'risks' && (
        <div className="dashboard-card full-width">
          <h2>Proje Riskleri</h2>
          <table>
            <thead>
              <tr>
                <th>Risk Başlığı</th>
                <th>Kategori</th>
                <th>Olasılık (1-5)</th>
                <th>Etki (1-5)</th>
                <th>Risk Puanı</th>
                <th>Sorumlu</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {risks.length > 0 ? (
                risks.map((r, idx) => {
                  const score = (r.probability || 0) * (r.impact || 0)
                  return (
                    <tr key={r.id || idx}>
                      <td className="fw-medium">{r.title}</td>
                      <td>{r.category || '-'}</td>
                      <td>{r.probability || '-'}</td>
                      <td>{r.impact || '-'}</td>
                      <td>
                        <span className={`risk-score-badge ${score >= 16 ? 'high' : score >= 8 ? 'mid' : 'low'}`}>
                          {score || '-'}
                        </span>
                      </td>
                      <td>{r.owner || '-'}</td>
                      <td>{r.status || 'Açık'}</td>
                    </tr>
                  )
                })
              ) : (
                <tr><td colSpan={7} style={{ textAlign: 'center' }}>Tanımlı risk bulunmuyor.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* 🚨 4. SEKME: SORUNLAR */}
      {activeTab === 'issues' && (
        <div className="dashboard-card full-width">
          <h2>Proje Sorunları</h2>
          <table>
            <thead>
              <tr>
                <th>Sorun Başlığı</th>
                <th>Öncelik</th>
                <th>Hedef Tarih</th>
                <th>Sorumlu</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {issues.length > 0 ? (
                issues.map((i, idx) => (
                  <tr key={i.id || idx}>
                    <td className="fw-medium">{i.title}</td>
                    <td>{i.priority || 'Normal'}</td>
                    <td>{formatDate(i.dueDate)}</td>
                    <td>{i.owner || '-'}</td>
                    <td>{i.status || 'Açık'}</td>
                  </tr>
                ))
              ) : (
                <tr><td colSpan={5} style={{ textAlign: 'center' }}>Tanımlı sorun bulunmuyor.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* ✅ 5. SEKME: AKSİYONLAR */}
      {activeTab === 'actions' && (
        <div className="dashboard-card full-width">
          <h2>Aksiyon Listesi</h2>
          <table>
            <thead>
              <tr>
                <th>Açıklama</th>
                <th>Kaynak</th>
                <th>Sorumlu</th>
                <th>Hedef Tarih</th>
                <th>İlerleme</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {actions.length > 0 ? (
                actions.map((a, idx) => (
                  <tr key={a.id || idx}>
                    <td className="fw-medium">{a.description}</td>
                    <td>{a.sourceType || 'Genel'}</td>
                    <td>{a.owner || '-'}</td>
                    <td>{formatDate(a.dueDate)}</td>
                    <td>%{a.progress || 0}</td>
                    <td>{a.status || 'Devam Ediyor'}</td>
                  </tr>
                ))
              ) : (
                <tr><td colSpan={6} style={{ textAlign: 'center' }}>Tanımlı aksiyon bulunmuyor.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* 📈 6. SEKME: EVM (KAZANILMIŞ DEĞER YÖNETİMİ) */}
      {activeTab === 'evm' && (
        <div className="dashboard-card full-width">
          <h2>EVM Göstergeleri ve Hesaplamaları</h2>
          <div className="dashboard-grid" style={{ marginTop: '16px' }}>
            <div className="dashboard-card">
              <p><strong>BAC (Tamamlanma Bütçesi):</strong> {evm.bac ?? '-'}</p>
              <p><strong>PV (Planlanan Değer):</strong> {evm.pv ?? '-'}</p>
              <p><strong>EV (Kazanılan Değer):</strong> {evm.ev ?? '-'}</p>
              <p><strong>AC (Gerçekleşen Maliyet):</strong> {evm.ac ?? '-'}</p>
            </div>
            <div className="dashboard-card">
              <p><strong>SV (Takvim Sapması):</strong> {evm.sv ?? '-'}</p>
              <p><strong>CV (Maliyet Sapması):</strong> {evm.cv ?? '-'}</p>
              <p><strong>EAC (Tahmini Tamamlanma Maliyeti):</strong> {evm.eac ?? '-'}</p>
              <p><strong>VAC (Tamamlanma Sapması):</strong> {evm.vac ?? '-'}</p>
            </div>
          </div>
        </div>
      )}

      {/* 📅 7. SEKME: PİR DÖNEMLERİ */}
      {activeTab === 'reports' && (
        <div className="dashboard-card full-width">
          <h2>Aylık PİR Rapor Dönemleri</h2>
          <table>
            <thead>
              <tr>
                <th>Dönem</th>
                <th>Rapor Tarihi</th>
                <th>Durum</th>
                <th>Yönetici Özeti</th>
              </tr>
            </thead>
            <tbody>
              {pirReports.length > 0 ? (
                pirReports.map((rep, idx) => (
                  <tr key={rep.id || idx}>
                    <td className="fw-bold">{rep.period}</td>
                    <td>{formatDate(rep.reportDate)}</td>
                    <td>{rep.status || 'Yayımlandı'}</td>
                    <td>{rep.executiveSummary ? `${rep.executiveSummary.slice(0, 60)}...` : '-'}</td>
                  </tr>
                ))
              ) : (
                <tr><td colSpan={4} style={{ textAlign: 'center' }}>Henüz PİR rapor dönemi girilmemiş.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

export default ProjectDetailPage