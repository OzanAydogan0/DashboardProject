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

  // URL üzerindeki 'tab' sorgu parametresini okuyoruz (Örn: ?tab=risks)
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

  // 3. VERİ ÇEKME İŞLEMİ (API CALLS) - Bileşen Düzeyinde Tanımlandı
  const fetchProjectData = async () => {
    if (!id) return

    try {
      setLoading(true)
      setError(null)
      
      // Ana Proje Detayı
      const projectDetailData = await projectService.getProjectById(id)

      // Tüm Alt Tablo Verilerini Paralel Olarak Çekme (Promise.allSettled)
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

  // Sayfa yüklendiğinde veya ID değiştiğinde verileri çek
  useEffect(() => {
    fetchProjectData()
  }, [id])

  // 4. BİÇİMLENDİRME VE YARDIMCI FONKSİYONLAR
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

  // Yüklenme ve Hata Durumları
  if (loading) return <div className="page-content"><div className="loading-state">Yükleniyor...</div></div>
  if (error || !project) return <div className="page-content"><div className="error-state">{error || 'Proje verisi bulunamadı.'}</div></div>

  // 5. MÜŞTERİ VE YÖNETİCİ İSİMLERİNİ EŞLEŞTİRME
  const customerObj = subData.customers.find(
    c => c.customerId === project.customerId || c.CustomerId === project.customerId
  )
  const foundCustomerName = customerObj ? (customerObj.customerName || customerObj.CustomerName) : null
  const displayCustomer = foundCustomerName 
    ? `${foundCustomerName} (${project.customerId})` 
    : project.customerId || '-'

  const tempManagerDictionary = {
    "USR-PM1": "Ahmet Yılmaz", 
    "USR-PM2": "Ayşe Demir"
  }
  const foundManagerName = project.projectManagerName || tempManagerDictionary[project.projectManagerUserId]
  const displayManager = foundManagerName 
    ? `${foundManagerName} (${project.projectManagerUserId})` 
    : project.projectManagerUserId || '-'

  // EVM ve Genel Bakış Metrikleri
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

  const maxEvmValue = Math.max(pv, ev, ac, 1)
  const pvHeight = (pv / maxEvmValue) * 100
  const evHeight = (ev / maxEvmValue) * 100
  const acHeight = (ac / maxEvmValue) * 100

  return (
    <div className="page-content project-detail-container">
      
      {/* ÜST BAŞLIK VE METADATA */}
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

        {/* SEKMELER (NAVBAR) */}
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
          <div className="kpi-grid">
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>SV (Zaman Sapması)</h3><p className={`kpi-value ${sv >= 0 ? 'text-success' : 'text-danger'}`}>{formatCurrency(sv, project.currency)}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>CV (Maliyet Sapması)</h3><p className={`kpi-value ${cv >= 0 ? 'text-success' : 'text-danger'}`}>{formatCurrency(cv, project.currency)}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>SPI (Zaman Performansı)</h3><p className={`kpi-value ${getSpiCpiClass(spi)}`}>{spi}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>CPI (Maliyet Performansı)</h3><p className={`kpi-value ${getSpiCpiClass(cpi)}`}>{cpi}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>Bitiş Sapması</h3><p className={`kpi-value ${getFinishVarianceClass(finishVarianceDays)}`}>{finishVarianceDays !== '-' ? `${finishVarianceDays} Gün` : '-'}</p></div></div>
            <div className="kpi-card hover-lift"><div className="kpi-details"><h3>Sağlık Durumu</h3><div className="kpi-value"><span className={`badge-health badge-${(project.manualHealth || 'yesil').toLowerCase()}`}>{project.manualHealth || 'Yeşil'}</span></div></div></div>
          </div>

          <div className="dashboard-grid main-content-grid">
            <div className="grid-left-col">
              <div className="dashboard-card shadow-card">
                <div className="card-header"><h2>Yönetici Özeti</h2><span className="period-badge">{latestReport?.period || 'Son Dönem'}</span></div>
                <div className="summary-text-box"><p>{executiveSummaryText}</p></div>
              </div>
              <div className="dashboard-card shadow-card">
                <div className="card-header"><h2>Yol Haritası (Yaklaşan Dönüm Noktaları)</h2><span className="period-badge">Bugün: {formatDate(new Date().toISOString())}</span></div>
                <div className="modern-timeline">
                  <div className="timeline-track"></div>
                  {subData.milestones.slice(0, 4).map((m, idx) => (
                    <div key={idx} className="timeline-node">
                      <div className={`node-point ${m.critical ? 'critical' : 'normal'}`}>{m.critical && <span className="pulse-ring"></span>}</div>
                      <div className="node-content"><h4 className="node-title">{m.milestoneName}</h4><span className="node-date">{formatDate(m.forecastDate || m.plannedDate)}</span></div>
                    </div>
                  ))}
                  {subData.milestones.length === 0 && <p className="text-muted">Planlanmış kilometre taşı yok.</p>}
                </div>
              </div>
            </div>
            <div className="grid-right-col">
              <div className="dashboard-card shadow-card evm-chart-card">
                <div className="card-header"><h2>Kazanılmış Değer Grafiği</h2><span className="period-badge">{latestEvm.period || 'Mevcut Dönem'}</span></div>
                <div className="modern-bar-chart">
                  <div className="chart-bars">
                    <div className="bar-group"><div className="bar pv-bar" style={{ '--bar-height': `${Math.max(pvHeight, 2)}%` }}><span className="bar-tooltip">{formatCurrency(pv, project.currency)}</span></div><span className="bar-label">PV</span></div>
                    <div className="bar-group"><div className="bar ev-bar" style={{ '--bar-height': `${Math.max(evHeight, 2)}%` }}><span className="bar-tooltip">{formatCurrency(ev, project.currency)}</span></div><span className="bar-label">EV</span></div>
                    <div className="bar-group"><div className="bar ac-bar" style={{ '--bar-height': `${Math.max(acHeight, 2)}%` }}><span className="bar-tooltip">{formatCurrency(ac, project.currency)}</span></div><span className="bar-label">AC</span></div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* SEKME 2: KİLOMETRE TAŞLARI */}
      {activeTab === 'milestones' && (
        <MileStone 
          milestones={subData.milestones} 
          projectId={project.id} 
          onMilestoneAdded={fetchProjectData}
          onMilestoneUpdated={fetchProjectData}
        />
      )}

      {/* SEKME 3: RİSKLER */}
      {activeTab === 'risks' && (
        <ProjectRisksPage risks={subData.risks} />
      )}

      {/* SEKME 4: SORUNLAR */}
      {activeTab === 'issues' && (
        <ProblemsPage issues={subData.issues} />
      )}

      {/* SEKME 5: AKSİYONLAR */}
      {activeTab === 'actions' && (
        <ActionsPage actions={subData.actions} />
      )}

      {/* SEKME 6: EVM VERİLERİ */}
      {activeTab === 'evm' && (
        <EvmRecordsPage evmRecords={subData.evmRecords} currency={project.currency} />
      )}

      {/* SEKME 7: RAPORLAR */}
      {activeTab === 'reports' && (
        <ReportsPage reports={subData.reports} />
      )}

    </div>
  )
}

export default ProjectDetailPage