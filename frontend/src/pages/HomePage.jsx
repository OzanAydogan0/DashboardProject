import { useState, useEffect, useMemo } from 'react'
import { projectService } from '../services/projectService'
import './HomePage.css'

function HomePage() {
  const [activeTab, setActiveTab] = useState('overview')
  const [rawProjects, setRawProjects] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Filtre State'leri
  const [filters, setFilters] = useState({
    projectCode: '',
    health: '',
    status: ''
  })

  // 🔄 1. İlk Sayfa Yüklenmesi (useEffect için senkron setState barındırmayan yapı)
  useEffect(() => {
    let isMounted = true

    const loadInitialData = async () => {
      try {
        const data = await projectService.getPortfolioDashboard()
        if (isMounted) {
          setRawProjects(Array.isArray(data) ? data : [])
        }
      } catch (err) {
        console.error('API Veri çekme hatası:', err)
        if (isMounted) {
          setError('Veriler backend servisinden alınamadı. Lütfen bağlantınızı kontrol edin.')
        }
      } finally {
        if (isMounted) {
          setLoading(false)
        }
      }
    }

    loadInitialData()

    return () => {
      isMounted = false
    }
  }, [])

  // 🔄 2. Kullanıcı Etkileşimi ile Yenileme veya Filtreleme (Buton tıklamaları için)
  const handleRefetch = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await projectService.getPortfolioDashboard(filters)
      setRawProjects(Array.isArray(data) ? data : [])
    } catch (err) {
      console.error('API Veri çekme hatası:', err)
      setError('Veriler backend servisinden alınamadı. Lütfen bağlantınızı kontrol edin.')
    } finally {
      setLoading(false)
    }
  }

  // 🔍 Client-Side veya Reaktif Filtreleme
  const filteredProjects = useMemo(() => {
    return rawProjects.filter((p) => {
      const matchCode = !filters.projectCode || p.projectCode === filters.projectCode
      const matchHealth =
        !filters.health ||
        (p.manualHealth || '').toLowerCase().includes(filters.health.toLowerCase())
      const matchStatus = !filters.status || p.projectStatus === filters.status

      return matchCode && matchHealth && matchStatus
    })
  }, [rawProjects, filters])

  // 🧮 Gelen DTO Verisinden Dinamik KPI Hesaplamaları
  const kpis = useMemo(() => {
    const total = filteredProjects.length
    if (total === 0) {
      return {
        totalProjects: 0,
        redProjects: 0,
        greenProjects: 0,
        yellowProjects: 0,
        delayedProjects: 0,
        avgPlannedProgress: 0,
        avgActualProgress: 0,
        criticalRisks: 0,
        delayedActions: 0
      }
    }

    let red = 0, green = 0, yellow = 0
    let sumPlanned = 0, sumActual = 0
    let totalRisks = 0, totalActions = 0, delayedCount = 0

    filteredProjects.forEach((p) => {
      const h = (p.manualHealth || '').toLowerCase()
      if (h.includes('kırmızı') || h.includes('kirmizi') || h.includes('red')) red++
      else if (h.includes('sarı') || h.includes('sari') || h.includes('yellow')) yellow++
      else green++

      const planned = p.plannedProgress || 0
      const actual = p.actualProgress || 0

      sumPlanned += planned
      sumActual += actual

      if (actual < planned) delayedCount++

      totalRisks += p.openRiskCount || 0
      totalActions += p.openActionCount || 0
    })

    return {
      totalProjects: total,
      redProjects: red,
      greenProjects: green,
      yellowProjects: yellow,
      delayedProjects: delayedCount,
      avgPlannedProgress: Math.round(sumPlanned / total),
      avgActualProgress: Math.round(sumActual / total),
      criticalRisks: totalRisks,
      delayedActions: totalActions
    }
  }, [filteredProjects])

  // 🍩 DONUT GRAFİK SVG MATEMATİĞİ
  const totalHealth = kpis.greenProjects + kpis.yellowProjects + kpis.redProjects || 1
  const greenPct = (kpis.greenProjects / totalHealth) * 100
  const yellowPct = (kpis.yellowProjects / totalHealth) * 100
  const redPct = (kpis.redProjects / totalHealth) * 100

  const greenOffset = 25
  const yellowOffset = 25 - greenPct
  const redOffset = 25 - greenPct - yellowPct

  // 📅 Tarih Sapma Hesaplama (Baseline Finish vs Forecast Finish)
  const calculateDeviation = (baselineStr, forecastStr) => {
    if (!baselineStr || !forecastStr) return '0'
    const b = new Date(baselineStr)
    const f = new Date(forecastStr)
    const diffDays = Math.ceil((f.getTime() - b.getTime()) / (1000 * 60 * 60 * 24))
    return diffDays > 0 ? `+${diffDays}` : `${diffDays}`
  }

  const handleFilterChange = (e) => {
    const { name, value } = e.target
    setFilters((prev) => ({ ...prev, [name]: value }))
  }

  const handleFilterSubmit = (e) => {
    e.preventDefault()
    handleRefetch()
  }

  return (
    <div className="homepage-content">
      {/* 🔴 ÜST BAŞLIK VE SEKME ALANI */}
      <div className="top-header-bar">
        <div className="header-title-tabs">
          <div className="header-tabs">

          </div>
        </div>

        <div className="top-user-actions">
          <button className="icon-btn" title="Yenile" onClick={handleRefetch}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M23 4v6h-6"></path><path d="M1 20v-6h6"></path><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
          </button>
        </div>
      </div>

      {/* 🔍 FİLTRE ÇUBUĞU */}
      <form className="dashboard-filter-bar" onSubmit={handleFilterSubmit}>
        <div className="filter-item filter-icon-label">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#64748B" strokeWidth="2"><line x1="4" y1="6" x2="20" y2="6"></line><line x1="8" y1="12" x2="16" y2="12"></line><line x1="10" y1="18" x2="14" y2="18"></line></svg>
        </div>

        <div className="filter-item">
          <select name="projectCode" value={filters.projectCode} onChange={handleFilterChange}>
            <option value="">Tüm Projeler</option>
            {rawProjects.map((p) => (
              <option key={p.projectId || p.projectCode} value={p.projectCode}>
                {p.projectCode} - {p.projectName}
              </option>
            ))}
          </select>
        </div>

        <div className="filter-item">
          <select name="status" value={filters.status} onChange={handleFilterChange}>
            <option value="">Tüm Durumlar</option>
            {Array.from(new Set(rawProjects.map((p) => p.projectStatus).filter(Boolean))).map((st) => (
              <option key={st} value={st}>{st}</option>
            ))}
          </select>
        </div>

        <div className="filter-item">
          <select name="health" value={filters.health} onChange={handleFilterChange}>
            <option value="">Tüm Sağlık Durumları</option>
            <option value="Yeşil">Yeşil</option>
            <option value="Sarı">Sarı</option>
            <option value="Kırmızı">Kırmızı</option>
          </select>
        </div>

        <button type="submit" className="btn-filter-submit" disabled={loading}>
          {loading ? 'Yükleniyor...' : 'Filtrele'}
        </button>
      </form>

      {error && (
        <div style={{ color: '#ef4444', padding: '12px', background: '#fee2e2', borderRadius: '8px', fontSize: '13px' }}>
          {error}
        </div>
      )}

      {/* 📊 1. SATIR: DİNAMİK KPI KARTLARI */}
      <div className="kpi-cards-grid">
        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Toplam Proje</span>
            <span className="kpi-value">{loading ? '...' : kpis.totalProjects}</span>
          </div>
          <div className="kpi-icon default">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Riskli Proje</span>
            <span className="kpi-value text-red">{loading ? '...' : kpis.redProjects}</span>
          </div>
          <div className="kpi-icon red">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Geciken Proje</span>
            <span className="kpi-value">{loading ? '...' : kpis.delayedProjects}</span>
          </div>
          <div className="kpi-icon orange">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Ortalama Plan %</span>
            <span className="kpi-value">%{loading ? '...' : kpis.avgPlannedProgress}</span>
          </div>
          <div className="kpi-icon default">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline><polyline points="17 6 23 6 23 12"></polyline></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Ortalama Gerçek %</span>
            <span className="kpi-value">%{loading ? '...' : kpis.avgActualProgress}</span>
          </div>
          <div className="kpi-icon blue">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Açık Risk Sayısı</span>
            <span className="kpi-value text-red">{loading ? '...' : kpis.criticalRisks}</span>
          </div>
          <div className="kpi-icon red">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-info">
            <span className="kpi-title">Açık Aksiyon</span>
            <span className="kpi-value">{loading ? '...' : kpis.delayedActions}</span>
          </div>
          <div className="kpi-icon yellow">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path></svg>
          </div>
        </div>
      </div>

      {/* 📈 2. SATIR: GRAFİKLER */}
      <div className="charts-two-columns">
        {/* Sol Grafik: Dinamik Donut Chart */}
        <div className="dashboard-card chart-card">
          <h3 className="card-title">Sağlık Dağılımı</h3>
          <div className="donut-chart-wrapper">
            <div className="donut-relative">
              <svg viewBox="0 0 42 42" className="donut-svg">
                <circle cx="21" cy="21" r="15.915" fill="transparent" stroke="#E2E8F0" strokeWidth="4.5" />
                {/* Yeşil Dilim */}
                <circle
                  cx="21" cy="21" r="15.915" fill="transparent"
                  stroke="#10B981" strokeWidth="4.5"
                  strokeDasharray={`${greenPct} ${100 - greenPct}`}
                  strokeDashoffset={greenOffset}
                />
                {/* Sarı Dilim */}
                <circle
                  cx="21" cy="21" r="15.915" fill="transparent"
                  stroke="#F59E0B" strokeWidth="4.5"
                  strokeDasharray={`${yellowPct} ${100 - yellowPct}`}
                  strokeDashoffset={yellowOffset}
                />
                {/* Kırmızı Dilim */}
                <circle
                  cx="21" cy="21" r="15.915" fill="transparent"
                  stroke="#EF4444" strokeWidth="4.5"
                  strokeDasharray={`${redPct} ${100 - redPct}`}
                  strokeDashoffset={redOffset}
                />
              </svg>
              <div className="donut-center-text">
                <span className="donut-number">{kpis.totalProjects}</span>
                <span className="donut-label">Proje</span>
              </div>
            </div>

            <div className="chart-legend">
              <div className="legend-item"><span className="dot green"></span> Yeşil ({kpis.greenProjects})</div>
              <div className="legend-item"><span className="dot yellow"></span> Sarı ({kpis.yellowProjects})</div>
              <div className="legend-item"><span className="dot red"></span> Kırmızı ({kpis.redProjects})</div>
            </div>
          </div>
        </div>

        {/* Sağ Grafik: Projeler İlerleme Karşılaştırması */}
        <div className="dashboard-card chart-card">
          <h3 className="card-title">Planlanan vs Gerçekleşen (Proje Bazlı)</h3>
          <div className="bar-chart-container">
            <div className="y-axis">
              <span>100</span>
              <span>75</span>
              <span>50</span>
              <span>25</span>
              <span>0</span>
            </div>
            <div className="bar-chart-body">
              <div className="grid-lines">
                <div></div><div></div><div></div><div></div><div></div>
              </div>
              <div className="bars-wrapper">
                {filteredProjects.slice(0, 5).map((p) => (
                  <div className="bar-group" key={p.projectId || p.projectCode}>
                    <div className="bars">
                      <div
                        className="bar planned"
                        style={{ height: `${p.plannedProgress || 0}%` }}
                        title={`Planlanan: %${p.plannedProgress}`}
                      ></div>
                      <div
                        className="bar actual"
                        style={{ height: `${p.actualProgress || 0}%` }}
                        title={`Gerçekleşen: %${p.actualProgress}`}
                      ></div>
                    </div>
                    <span className="x-label">{p.projectCode}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="bar-chart-legend">
            <div className="legend-item"><span className="square planned"></span> Planlanan %</div>
            <div className="legend-item"><span className="square actual"></span> Gerçekleşen %</div>
          </div>
        </div>
      </div>

      {/* 📋 3. SATIR: PROJE LİSTESİ TABLOSU */}
      <div className="dashboard-card table-card">
        <div className="table-header">
          <h3>Proje Listesi</h3>
        </div>

        <div className="table-responsive">
          <table className="pir-custom-table">
            <thead>
              <tr>
                <th>Kod</th>
                <th>Proje Adı</th>
                <th>Durum</th>
                <th>Plan %</th>
                <th>Gerçek %</th>
                <th>Açık Risk</th>
                <th>Açık Sorun</th>
                <th>Sapma (Gün)</th>
                <th>Sağlık</th>
                <th>Son Dönem</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="10" style={{ textAlign: 'center', padding: '20px' }}>Yükleniyor...</td>
                </tr>
              ) : filteredProjects.length > 0 ? (
                filteredProjects.map((prj) => {
                  const deviation = calculateDeviation(prj.baselineFinishDate, prj.forecastFinishDate)
                  const healthStr = prj.manualHealth || 'Yeşil'

                  return (
                    <tr key={prj.projectId || prj.projectCode}>
                      <td className="fw-bold">{prj.projectCode}</td>
                      <td className="fw-medium">{prj.projectName}</td>
                      <td>{prj.projectStatus}</td>
                      <td>%{prj.plannedProgress || 0}</td>
                      <td>%{prj.actualProgress || 0}</td>
                      <td className={prj.openRiskCount > 0 ? 'text-red fw-bold' : ''}>{prj.openRiskCount || 0}</td>
                      <td className={prj.openIssueCount > 0 ? 'text-orange fw-bold' : ''}>{prj.openIssueCount || 0}</td>
                      <td>{deviation}</td>
                      <td>
                        <span className={`badge-health badge-${healthStr.toLowerCase()}`}>
                          ● {healthStr}
                        </span>
                      </td>
                      <td className="text-muted">{prj.latestEvmPeriod || '-'}</td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td colSpan="10" style={{ textAlign: 'center', padding: '20px', color: '#64748b' }}>
                    Kriterlere uygun proje bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="table-footer">
          <span className="pagination-info">
            1 - {filteredProjects.length} / {rawProjects.length} Proje
          </span>
        </div>
      </div>
    </div>
  )
}

export default HomePage