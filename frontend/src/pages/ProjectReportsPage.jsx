import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { projectService } from '../services/projectService'
import './ProjectReportsPage.css'

const getCanWrite = () => {
  try {
    const userString = localStorage.getItem('user')
    const user = userString ? JSON.parse(userString) : null
    const userRole = user?.userRole || user?.UserRole || user?.role || user?.Role || ''
    const isExecutive = ['Üst Yönetim İzleyicisi', 'Üst Yönetim'].includes(userRole)
    return !isExecutive
  } catch {
    return false
  }
}

function ReportsPage({ reports = [], onRefresh }) {
  const { id: projectId } = useParams()
  const [form, setForm] = useState({
    period: '',
    reportDate: '',
    executiveSummary: '',
    completedWork: '',
    delays: '',
    nextPeriodPlan: '',
    managementExpectations: '',
    manualHealth: 'Sarı',
    reportStatus: 'Taslak'
  })
  const [editingId, setEditingId] = useState(null)
  const [showModal, setShowModal] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [canWrite] = useState(getCanWrite)

  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  const getStatusClass = (status) => {
    if (!status) return 'status-default'
    return status.toLowerCase().includes('yayıml') ? 'status-published' : 'status-draft'
  }

  const getHealthClass = (health) => {
    if (!health) return 'health-default'
    const value = health.toLowerCase()
    if (value.includes('kırmızı')) return 'health-danger'
    if (value.includes('yeşil')) return 'health-success'
    return 'health-warning'
  }

  const resetForm = () => {
    setEditingId(null)
    setForm({
      period: '',
      reportDate: '',
      executiveSummary: '',
      completedWork: '',
      delays: '',
      nextPeriodPlan: '',
      managementExpectations: '',
      manualHealth: 'Sarı',
      reportStatus: 'Taslak'
    })
  }

  const openCreateModal = () => {
    resetForm()
    setShowModal(true)
  }

  const openEditModal = (report) => {
    setEditingId(report.pirReportId || report.id)
    setForm({
      period: report.period || '',
      reportDate: report.reportDate ? new Date(report.reportDate).toISOString().slice(0, 10) : '',
      executiveSummary: report.executiveSummary || '',
      completedWork: report.completedWork || '',
      delays: report.delays || '',
      nextPeriodPlan: report.nextPeriodPlan || '',
      managementExpectations: report.managementExpectations || '',
      manualHealth: report.manualHealth || 'Sarı',
      reportStatus: report.reportStatus || 'Taslak'
    })
    setShowModal(true)
  }

  const closeModal = () => {
    setShowModal(false)
    resetForm()
  }

  const handleInputChange = (event) => {
    const { name, value } = event.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (!projectId) return
    setIsSubmitting(true)
    setError(null)

    try {
      const payload = {
        projectId,
        period: form.period,
        reportDate: form.reportDate ? new Date(form.reportDate).toISOString() : null,
        executiveSummary: form.executiveSummary,
        completedWork: form.completedWork,
        delays: form.delays || null,
        nextPeriodPlan: form.nextPeriodPlan,
        managementExpectations: form.managementExpectations || null,
        manualHealth: form.manualHealth,
        reportStatus: form.reportStatus
      }

      if (editingId) {
        await projectService.updateProjectReport(editingId, payload)
      } else {
        await projectService.createProjectReport(projectId, payload)
      }

      closeModal()
      if (onRefresh) await onRefresh()
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'Rapor işlenemedi.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleDelete = async (report) => {
    if (!window.confirm('Bu rapor dönemini silmek istediğinize emin misiniz?')) return
    try {
      await projectService.deleteProjectReport(report.pirReportId || report.id)
      if (onRefresh) await onRefresh()
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'Rapor silinemedi.')
    }
  }

  return (
    <div className="tab-content-wrapper fade-in reports-page-wrapper">
      <div className="dashboard-card shadow-card reports-page-card">
        <div className="card-header reports-card-header">
          <h2>Geçmiş Dönem Raporları</h2>
          {canWrite && (
            <button type="button" className="btn-primary reports-create-btn" onClick={openCreateModal}>
              Yeni Dönem
            </button>
          )}
        </div>

        {!canWrite && (
          <div className="permission-note">Bu projede rapor ekleme/düzenleme/silme yetkiniz bulunmuyor.</div>
        )}

        {error && <div className="error-state reports-error">{error}</div>}

        <div className="table-responsive reports-table-wrapper">
          <table className="modern-table reports-table">
            <thead>
              <tr>
                <th>Dönem</th>
                <th>Rapor Tarihi</th>
                <th>Durum</th>
                <th>Sağlık</th>
                <th>Özet</th>
                <th>Tamamlanan İş</th>
                <th>Gecikmeler</th>
                <th>Sonraki Plan</th>
                <th>Yönetim Beklentileri</th>
                {canWrite && <th>İşlemler</th>}
              </tr>
            </thead>
            <tbody>
              {reports.length > 0 ? reports.map((r, idx) => (
                <tr key={r.pirReportId || r.id || idx}>
                  <td className="font-medium">{r.period || '-'}</td>
                  <td>{formatDate(r.reportDate || r.createdAt)}</td>
                  <td>
                    <span className={`report-badge ${getStatusClass(r.reportStatus || r.status)}`}>
                      {r.reportStatus || r.status || '-'}
                    </span>
                  </td>
                  <td>
                    <span className={`report-badge ${getHealthClass(r.manualHealth)}`}>
                      {r.manualHealth || '-'}
                    </span>
                  </td>
                  <td>
                    <div className="report-cell-text" title={r.executiveSummary}>
                      {r.executiveSummary || '-'}
                    </div>
                  </td>
                  <td>
                    <div className="report-cell-text" title={r.completedWork}>
                      {r.completedWork || '-'}
                    </div>
                  </td>
                  <td>
                    <div className="report-cell-text" title={r.delays}>
                      {r.delays || '-'}
                    </div>
                  </td>
                  <td>
                    <div className="report-cell-text" title={r.nextPeriodPlan}>
                      {r.nextPeriodPlan || '-'}
                    </div>
                  </td>
                  <td>
                    <div className="report-cell-text" title={r.managementExpectations}>
                      {r.managementExpectations || '-'}
                    </div>
                  </td>
                  {canWrite && (
                    <td>
                      <div className="report-actions">
                        <button type="button" className="btn-secondary report-action-btn" onClick={() => openEditModal(r)}>
                          Düzenle
                        </button>
                        <button type="button" className="btn-danger report-action-btn" onClick={() => handleDelete(r)}>
                          Sil
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              )) : (
                <tr>
                  <td colSpan={canWrite ? '10' : '9'} className="reports-empty-state">
                    Kayıt bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {showModal && canWrite && (
        <div className="issue-modal-overlay" onClick={closeModal}>
          <div className="issue-modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 720 }}>
            <div className="issue-modal-header">
              <h3>{editingId ? 'Rapor Dönemi Düzenle' : 'Yeni Rapor Dönemi Ekle'}</h3>
              <button type="button" className="modal-close-btn" onClick={closeModal}>×</button>
            </div>

            <form className="issue-form" onSubmit={handleSubmit}>
              <div className="form-grid">
                <label>
                  <span>Dönem</span>
                  <input name="period" value={form.period} onChange={handleInputChange} required />
                </label>
                <label>
                  <span>Rapor Tarihi</span>
                  <input name="reportDate" type="date" value={form.reportDate} onChange={handleInputChange} required />
                </label>
                <label>
                  <span>Durum</span>
                  <select name="reportStatus" value={form.reportStatus} onChange={handleInputChange}>
                    <option value="Taslak">Taslak</option>
                    <option value="Yayımlandı">Yayımlandı</option>
                  </select>
                </label>
                <label>
                  <span>Sağlık Durumu</span>
                  <select name="manualHealth" value={form.manualHealth} onChange={handleInputChange}>
                    <option value="Kırmızı">Kırmızı</option>
                    <option value="Sarı">Sarı</option>
                    <option value="Yeşil">Yeşil</option>
                  </select>
                </label>
                <label style={{ gridColumn: '1 / -1' }}>
                  <span>Yönetici Özeti</span>
                  <textarea name="executiveSummary" value={form.executiveSummary} onChange={handleInputChange} rows={3} required />
                </label>
                <label style={{ gridColumn: '1 / -1' }}>
                  <span>Tamamlanan İş</span>
                  <textarea name="completedWork" value={form.completedWork} onChange={handleInputChange} rows={3} required />
                </label>
                <label style={{ gridColumn: '1 / -1' }}>
                  <span>Gecikmeler</span>
                  <textarea name="delays" value={form.delays} onChange={handleInputChange} rows={2} />
                </label>
                <label style={{ gridColumn: '1 / -1' }}>
                  <span>Sonraki Dönem Planı</span>
                  <textarea name="nextPeriodPlan" value={form.nextPeriodPlan} onChange={handleInputChange} rows={2} required />
                </label>
                <label style={{ gridColumn: '1 / -1' }}>
                  <span>Yönetim Beklentileri</span>
                  <textarea name="managementExpectations" value={form.managementExpectations} onChange={handleInputChange} rows={2} />
                </label>
              </div>
              <div className="issue-modal-actions" style={{ marginTop: 16 }}>
                <button type="button" className="btn-secondary" onClick={closeModal}>İptal</button>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting ? 'Kaydediliyor...' : editingId ? 'Güncelle' : 'Ekle'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

export default ReportsPage