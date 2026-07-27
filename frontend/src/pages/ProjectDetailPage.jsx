import { useState, useEffect } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { projectService } from '../services/projectService'
import './ProjectDetailPage.css'
import MileStone from './MileStonePage'
import ProjectRisksPage from './ProjectRisksPage'
import ProblemsPage from './ProblemsPage'
import ActionsPage from './ProjectActionsPage'
import EvmRecordsPage from './EvmRecordsPage'
import ReportsPage from './ProjectReportsPage'

function ProjectDetailPage() {
  // 1. ROUTER VE URL PARAMETRELERİ
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()

  const queryParams = new URLSearchParams(location.search)
  const activeTab = queryParams.get('tab') || 'overview'
  
  // 2. STATE (DURUM) YÖNETİMİ
  const [project, setProject] = useState(null)
  const [subData, setSubData] = useState({
    milestones: [],
    risks: [],
    issues: [],
    actions: [],
    reports: [],
    evmRecords: [],
    customers: []
  })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // 3. VERİ ÇEKME İŞLEMİ
  const fetchProjectData = async () => {
    if (!id) return

    try {
      setLoading(true)
      setError(null)
      
      const projectDetailData = await projectService.getProjectById(id)

      const [milestonesRes, risksRes, issuesRes, actionsRes, reportsRes, evmRes, customersRes] = await Promise.allSettled([
        projectService.getProjectMilestones(id),
        projectService.getProjectRisks(id),
        projectService.getProjectIssues(id),
        projectService.getProjectActions(id),
        projectService.getProjectReports(id),
        projectService.getEvmRecords(id),
        projectService.getCustomers()
      ])

      setProject(projectDetailData)
      setSubData({
        milestones: milestonesRes.status === 'fulfilled' && Array.isArray(milestonesRes.value) ? milestonesRes.value : [],
        risks: risksRes.status === 'fulfilled' && Array.isArray(risksRes.value) ? risksRes.value : [],
        issues: issuesRes.status === 'fulfilled' && Array.isArray(issuesRes.value) ? issuesRes.value : [],
        actions: actionsRes.status === 'fulfilled' && Array.isArray(actionsRes.value) ? actionsRes.value : [],
        reports: reportsRes.status === 'fulfilled' && Array.isArray(reportsRes.value) ? reportsRes.value : [],
        evmRecords: evmRes.status === 'fulfilled' && Array.isArray(evmRes.value) ? evmRes.value : [],
        customers: customersRes.status === 'fulfilled' && Array.isArray(customersRes.value) ? customersRes.value : []
      })
    } catch (err) {
      const backendMessage = err.response?.data?.message || err.message
      if (err.response?.status === 403) setError(backendMessage || 'Bu projeyi görme yetkiniz yok!')
      else setError('Proje detayları veritabanından çekilemedi.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchProjectData()
  }, [id])

  // 4. BİÇİMLENDİRME YARDIMCILARI
  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  const formatCurrency = (val, currency = 'TRY') => {
    if (val == null) return '-'
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(val)
  }

  const getSpiCpiClass = (val) => {
    if (val >= 0.95) return 'text-success' 
    if (val >= 0.85) return 'text-warning' 
    return 'text-danger'                   
  }

  const getFinishVarianceClass = (days) => {
    if (days === '-' || isNaN(days)) return 'text-neutral' 
    if (days <= 15) return 'text-success'  
    if (days <= 45) return 'text-warning'  
    return 'text-danger'                   
  }

  if (loading) return <div className="page-content"><div className="loading-state">Yükleniyor...</div></div>
  if (error || !project) return <div className="page-content"><div className="error-state">{error || 'Proje verisi bulunamadı.'}</div></div>

  // Müşteri & Yönetici Bilgileri
  const customerObj = subData.customers.find(c => c.customerId === project.customerId || c.CustomerId === project.customerId)
  const displayCustomer = customerObj ? `${customerObj.customerName || customerObj.CustomerName} (${project.customerId})` : project.customerId || '-'

  const tempManagerDictionary = { "USR-PM1": "Ahmet Yılmaz", "USR-PM2": "Ayşe Demir" }
  const displayManager = project.projectManagerName || tempManagerDictionary[project.projectManagerUserId] || project.projectManagerUserId || '-'

  // EVM Metrikleri
  const latestEvm = subData.evmRecords[0] || {}
  const pv = latestEvm.pv ?? 0
  const ev = latestEvm.ev ?? 0
  const ac = latestEvm.ac ?? 0
  const spi = latestEvm.spi ?? 0
  const cpi = latestEvm.cpi ?? 0
  const sv = latestEvm.sv ?? 0
  const cv = latestEvm.cv ?? 0

  const finishVarianceDays = project.forecastFinishDate && project.baselineFinishDate 
    ? Math.floor((new Date(project.forecastFinishDate) - new Date(project.baselineFinishDate)) / (1000 * 60 * 60 * 24))
    : '-'

  const latestReport = subData.reports.find(r => r.status === 'Yayımlandı') || subData.reports[0]
  const executiveSummaryText = latestReport?.executiveSummary || 'Bu proje için henüz yayımlanmış bir PİR dönemi özeti bulunmamaktadır.'

  // Yatay EVM Yüzdeleri
  const maxEvmValue = Math.max(pv, ev, ac, 1)
  const pvWidth = (pv / maxEvmValue) * 100
  const evWidth = (ev / maxEvmValue) * 100
  const acWidth = (ac / maxEvmValue) * 100

  // -------------------------------------------------------------
  // KRONOLOJİK SIRALAMA VE DİNAMİK HASSAS "BUGÜN" ÇİZGİSİ HESABI
  // -------------------------------------------------------------
  const today = new Date()

  // 1. Dönüm noktalarını tarihlerine göre kronolojik sıralayalım
  const sortedMilestones = [...subData.milestones].sort((a, b) => {
    const dateA = new Date(a.forecastDate || a.plannedDate || 0).getTime()
    const dateB = new Date(b.forecastDate || b.plannedDate || 0).getTime()
    return dateA - dateB
  })

  // 2. Bugün çizgisinin ekrandaki kartların görsel konumuna göre yüzdesini bulalım
  const calculateTodayPercent = () => {
    const count = sortedMilestones.length
    if (count === 0) return 50

    const todayTime = today.getTime()

    // Space-around düzeninde her i. kartın ekranın yüzde kaçında olduğunu verir
    const getNodePercent = (index) => ((index + 0.5) / count) * 100

    const firstDate = new Date(sortedMilestones[0].forecastDate || sortedMilestones[0].plannedDate).getTime()
    const lastDate = new Date(sortedMilestones[count - 1].forecastDate || sortedMilestones[count - 1].plannedDate).getTime()

    // Bugün ilk karttan da önceyse
    if (todayTime <= firstDate) {
      return Math.max(2, getNodePercent(0) - 10)
    }
    // Bugün son karttan da sonraysa
    if (todayTime >= lastDate) {
      return Math.min(98, getNodePercent(count - 1) + 10)
    }

    // Bugün iki kart arasındaysa oransal interpolasyon yap
    for (let i = 0; i < count - 1; i++) {
      const d1 = new Date(sortedMilestones[i].forecastDate || sortedMilestones[i].plannedDate).getTime()
      const d2 = new Date(sortedMilestones[i + 1].forecastDate || sortedMilestones[i + 1].plannedDate).getTime()

      if (todayTime >= d1 && todayTime <= d2) {
        const p1 = getNodePercent(i)
        const p2 = getNodePercent(i + 1)
        const ratio = (d2 - d1) > 0 ? (todayTime - d1) / (d2 - d1) : 0
        return p1 + ratio * (p2 - p1)
      }
    }

    return 50
  }

  const todayPercent = calculateTodayPercent()

  return (
    <div className="page-content project-detail-container">
      
      {/* ÜST BAŞLIK VE NAVİGASYON */}
      <div className="dashboard-header project-detail-header">
        <div className="header-top-row">
          <button className="back-btn" onClick={() => navigate('/projects')}>
            ← Projelere Dön
          </button>
        </div>

        <div className="project-title-section">
          <h1>{project.projectName || 'İsimsiz Proje'}</h1>
          <div className="project-meta-pills">
            <span className="pill"><strong>Kod:</strong> {project.projectCode || '-'}</span>
            <span className="pill"><strong>Müşteri:</strong> {displayCustomer}</span>
            <span className="pill"><strong>Yönetici:</strong> {displayManager}</span>
            <span className={`pill status-${(project.projectStatus || 'taslak').toLowerCase().replace(/\s+/g, '-')}`}>
              {project.projectStatus || 'Taslak'}
            </span>
          </div>
        </div>

        <div className="project-nav-tabs">
          {['overview', 'milestones', 'risks', 'issues', 'actions', 'evm', 'reports'].map(tab => (
            <button 
              key={tab}
              className={`tab-item ${activeTab === tab ? 'active' : ''}`} 
              onClick={() => navigate(`/projects/${id}?tab=${tab}`)}>
              {tab === 'overview' ? 'Genel Bakış' :
               tab === 'milestones' ? 'Kilometre Taşları' :
               tab === 'risks' ? 'Riskler' :
               tab === 'issues' ? 'Sorunlar' :
               tab === 'actions' ? 'Aksiyonlar' :
               tab === 'evm' ? 'EVM Verileri' : 'Rapor Dönemleri'}
            </button>
          ))}
        </div>
      </div>

      {/* SEKME 1: GENEL BAKIŞ */}
      {activeTab === 'overview' && (
        <div className="tab-content-wrapper fade-in">
          
          {/* METRİK KARTLARI */}
          <div className="kpi-grid">
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>SV (Zaman Sapması)</h3><p className={`kpi-value ${sv >= 0 ? 'text-success' : 'text-danger'}`}>{formatCurrency(sv, project.currency)}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>CV (Maliyet Sapması)</h3><p className={`kpi-value ${cv >= 0 ? 'text-success' : 'text-danger'}`}>{formatCurrency(cv, project.currency)}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>SPI (Zaman Performansı)</h3><p className={`kpi-value ${getSpiCpiClass(spi)}`}>{spi}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>CPI (Maliyet Performansı)</h3><p className={`kpi-value ${getSpiCpiClass(cpi)}`}>{cpi}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>Bitiş Sapması</h3><p className={`kpi-value ${getFinishVarianceClass(finishVarianceDays)}`}>{finishVarianceDays !== '-' ? `${finishVarianceDays} Gün` : '-'}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>Sağlık Durumu</h3><div className="kpi-value"><span className={`badge-health badge-${(project.manualHealth || 'yesil').toLowerCase()}`}>{project.manualHealth || 'Yeşil'}</span></div></div></div>
          </div>

          {/* DÜZEN: YÖNETİCİ ÖZETİ VE EVM KARTLARI */}
          <div className="dashboard-grid overview-top-grid">
            <div className="dashboard-card shadow-card">
              <div className="card-header"><h2>Yönetici Özeti</h2><span className="period-badge">{latestReport?.period || 'Son Dönem'}</span></div>
              <div className="summary-text-box"><p>{executiveSummaryText}</p></div>
            </div>

            <div className="dashboard-card shadow-card evm-chart-card-compact">
              <div className="card-header">
                <h2>Kazanılmış Değer Grafiği</h2>
                <span className="period-badge">{latestEvm.period || 'Mevcut Dönem'}</span>
              </div>

              <div className="modern-horizontal-bar-chart compact">
                <div className="horizontal-bar-group">
                  <span className="bar-label-left">PV</span>
                  <div className="bar-track">
                    <div className="bar pv-bar" style={{ '--bar-width': `${Math.max(pvWidth, 4)}%` }}>
                      <span className="bar-value-inside">{formatCurrency(pv, project.currency)}</span>
                    </div>
                  </div>
                </div>

                <div className="horizontal-bar-group">
                  <span className="bar-label-left">EV</span>
                  <div className="bar-track">
                    <div className="bar ev-bar" style={{ '--bar-width': `${Math.max(evWidth, 4)}%` }}>
                      <span className="bar-value-inside">{formatCurrency(ev, project.currency)}</span>
                    </div>
                  </div>
                </div>

                <div className="horizontal-bar-group">
                  <span className="bar-label-left">AC</span>
                  <div className="bar-track">
                    <div className="bar ac-bar" style={{ '--bar-width': `${Math.max(acWidth, 4)}%` }}>
                      <span className="bar-value-inside">{formatCurrency(ac, project.currency)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* ALT BÖLÜM: YOL HARİTASI (HASSAS HİZALANMIŞ DİKEY BUGÜN ÇİZGİSİ) */}
          <div className="dashboard-card shadow-card roadmap-full-width">
            <div className="card-header">
              <div>
                <h2>Yol Haritası (Zaman Eksenli Dönüm Noktaları)</h2>
                <p className="section-subtitle">Plan, Tahmin ve Gerçekleşen tarihler ile güncel durum göstergesi</p>
              </div>
              <span className="period-badge today-highlight">Bugün: {formatDate(today.toISOString())}</span>
            </div>

            <div className="timeline-scroll-wrapper">
              <div className="modern-timeline-axis">
                
                {/* YATAY EKSEN ÇİZGİSİ */}
                <div className="timeline-horizontal-track"></div>

                {/* DİNAMİK VE HASSAS HİZALANMIŞ "BUGÜN" ÇİZGİSİ */}
                <div className="today-vertical-line" style={{ left: `${todayPercent}%` }}>
                  <div className="today-flag">📍 BUGÜN</div>
                  <div className="today-dashed-line"></div>
                </div>

                {/* KRONOLOJİK SIRALI KİLOMETRE TAŞLARI */}
                <div className="milestones-nodes-container">
                  {sortedMilestones.map((m, idx) => (
                    <div key={idx} className="milestone-axis-card">
                      <div className={`milestone-status-dot ${m.critical ? 'critical' : 'normal'}`}>
                        {m.critical && <span className="pulse-ring"></span>}
                      </div>

                      <div className="milestone-card-body">
                        <h4 className="milestone-name">{m.milestoneName}</h4>
                        
                        <div className="milestone-dates-grid">
                          <div className="date-pill plan">
                            <span className="date-label">Plan:</span>
                            <span className="date-val">{formatDate(m.plannedDate)}</span>
                          </div>
                          <div className="date-pill forecast">
                            <span className="date-label">Tahmin:</span>
                            <span className="date-val">{formatDate(m.forecastDate)}</span>
                          </div>
                          <div className={`date-pill actual ${m.actualDate ? 'completed' : 'pending'}`}>
                            <span className="date-label">Gerçek:</span>
                            <span className="date-val">{formatDate(m.actualDate)}</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}

                  {sortedMilestones.length === 0 && (
                    <p className="text-muted" style={{ padding: '30px', textAlign: 'center', width: '100%' }}>
                      Henüz tanımlanmış bir kilometre taşı bulunmuyor.
                    </p>
                  )}
                </div>

              </div>
            </div>
          </div>

        </div>
      )}

      {/* DİĞER SEKMELER */}
      {activeTab === 'milestones' && <MileStone milestones={subData.milestones} projectId={project.projectId || project.ProjectId || id} onMilestoneAdded={fetchProjectData} onMilestoneUpdated={fetchProjectData} />}
      {activeTab === 'risks' && <ProjectRisksPage risks={subData.risks} />}
      {activeTab === 'issues' && <ProblemsPage issues={subData.issues} />}
      {activeTab === 'actions' && <ActionsPage actions={subData.actions} />}
      {activeTab === 'evm' && <EvmRecordsPage evmRecords={subData.evmRecords} currency={project.currency} />}
      {activeTab === 'reports' && <ReportsPage reports={subData.reports} onRefresh={fetchProjectData} />}

    </div>
  )
}

export default ProjectDetailPage