import React, { useState } from 'react'
import { projectService } from '../services/projectService'
import './MileStonePage.css'

/**
 * Kilometre Taşları (Milestones) Bileşeni
 * 
 * @param {Array} milestones - Veritabanından gelen mevcut kilometre taşları
 * @param {String|Number} projectId - İlişkili projenin ID değeri
 * @param {Function} onMilestoneUpdated - Veri eklendiğinde/güncellendiğinde listeyi tazeleyen callback
 * @param {Function} onMilestoneAdded - Alternatif güncelleme callback'i
 */
function MileStone({ milestones = [], projectId, onMilestoneUpdated, onMilestoneAdded }) {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  
  const [loading, setLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const [formData, setFormData] = useState({
    milestoneName: '',
    plannedDate: '',
    actualDate: '',
    status: 'Planlandı'
  })

  const handleInputChange = (e) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
  }

  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  const formatForInput = (dateString) => {
    if (!dateString) return ''
    return new Date(dateString).toISOString().split('T')[0]
  }

  const getMilestoneStatusStyle = (status) => {
    const s = (status || '').toLowerCase()
    if (s.includes('tamamlandı')) return 'status-completed'
    if (s.includes('devam')) return 'status-inprogress'
    return 'status-planned'
  }

  const handleOpenAddModal = () => {
    setEditingId(null)
    setErrorMessage('')
    setFormData({
      milestoneName: '',
      plannedDate: '',
      actualDate: '',
      status: 'Planlandı'
    })
    setIsModalOpen(true)
  }

  const handleOpenEditModal = (item) => {
    setEditingId(item.id || item.milestoneId)
    setErrorMessage('')
    setFormData({
      milestoneName: item.milestoneName || '',
      plannedDate: formatForInput(item.plannedDate),
      actualDate: formatForInput(item.actualDate),
      status: item.milestoneStatus || item.status || 'Planlandı'
    })
    setIsModalOpen(true)
  }

  // --- VERİTABANINA KAYIT VE GÜNCELLEME İŞLEMİ ---
  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    setErrorMessage('')

    try {
      if (editingId) {
        // GÜNCELLEME (PATCH /milestones/{id})
        const updatePayload = {
          milestoneName: formData.milestoneName,
          plannedDate: formData.plannedDate,
          forecastDate: formData.plannedDate,
          actualDate: formData.actualDate || null,
          milestoneStatus: formData.status
        }
        await projectService.updateMilestone(editingId, updatePayload)
      } else {
        // YENİ EKLEME (POST /projects/{projectId}/milestones)
        // Backend CreateMilestoneRequest DTO yapısına tam uyumlu paket:
        const createPayload = {
          milestoneName: formData.milestoneName,
          plannedDate: formData.plannedDate,
          forecastDate: formData.plannedDate, // Varsayılan olarak planlanan tarih atanır
          milestoneStatus: formData.status,
          critical: 0,
          milestoneOwnerUserId: null,
          acceptanceCriteria: '',
          milestoneDescription: ''
        }

        await projectService.createProjectMilestone(projectId, createPayload)
      }

      // İşlem başarılıysa modalı kapat ve listeyi güncelle
      setIsModalOpen(false)
      
      if (onMilestoneUpdated) onMilestoneUpdated()
      if (onMilestoneAdded) onMilestoneAdded()

    } catch (error) {
      console.error('Veritabanı işlem hatası:', error)
      const errorMsg = error.response?.data?.message || 'İşlem sırasında bir hata oluştu.'
      setErrorMessage(errorMsg)
    } finally {
      setLoading(false)
    }
  }

  // Silme İşlemi Fonksiyonu
  const handleDelete = async (milestoneId) => {
    if (!milestoneId) {
      alert("Silinecek ögenin ID bilgisi bulunamadı.");
      return;
    }

    const isConfirmed = window.confirm("Bu kilometre taşını silmek istediğinize emin misiniz?");
    if (!isConfirmed) return;

    try {
      await projectService.deleteProjectMilestone(milestoneId);
      alert("Kilometre taşı başarıyla silindi!");
      
      // Listeyi yenile
      if (onMilestoneUpdated) onMilestoneUpdated();
      if (onMilestoneAdded) onMilestoneAdded();
    } catch (error) {
      console.error("Silme işlemi başarısız:", error);
      // Backend'den dönen özel hata mesajını gösterelim
      const serverErrorMsg = error.response?.data?.message || "Silme işlemi başarısız oldu. Oturumunuz kapanmış veya yetkiniz olmayabilir.";
      alert(serverErrorMsg);
    }
  };

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card timeline-card">
        
        <div className="timeline-header-actions">
          <div style={{ flex: 1 }}></div>
          <button 
            className="btn-add-milestone" 
            onClick={handleOpenAddModal}>
            + Yeni Milestone
          </button>
        </div>

        <div className="table-responsive">
          <table className="timeline-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th style={{ width: '50px' }}></th>
                <th>Milestone Adı</th>
                <th>Planlanan Tarih</th>
                <th>Gerçekleşen Tarih</th>
                <th>Durum</th>
                <th style={{ textAlign: 'right', paddingRight: '16px' }}>İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {milestones.length > 0 ? milestones.map((m, idx) => {
                const statusClass = getMilestoneStatusStyle(m.milestoneStatus || m.status)
                return (
                  <tr key={m.id || m.milestoneId || idx}>
                    <td className="timeline-td">
                      <div className="timeline-line"></div>
                      <div className={`timeline-dot ${statusClass}-dot`}></div>
                    </td>
                    <td className="font-medium">{m.milestoneName || '-'}</td>
                    <td>{formatDate(m.plannedDate)}</td>
                    <td>{formatDate(m.actualDate)}</td>
                    <td>
                      <span className={`milestone-badge ${statusClass}-badge`}>
                        {m.milestoneStatus || m.status || 'Planlandı'}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right', paddingRight: '16px' }}>
                      <button 
                        className="btn-action-edit"
                        onClick={() => handleOpenEditModal(m)}
                        title="Düzenle"
                      >
                        Düzenle
                      </button>

                     <button 
                      className="btn-delete-milestone" 
                      onClick={() => handleDelete(m.milestoneId || m.id)}
                    >
                      Sil
                    </button>

                    </td>
                  </tr>
                )
              }) : (
                <tr>
                  <td colSpan="6" style={{ textAlign: 'center', padding: '20px' }}>
                    Kayıt bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {isModalOpen && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>{editingId ? 'Milestone Düzenle' : 'Yeni Kilometre Taşı Ekle'}</h3>
            
            {errorMessage && (
              <div style={{ color: '#dc2626', backgroundColor: '#fee2e2', padding: '10px', borderRadius: '6px', marginBottom: '15px' }}>
                {errorMessage}
              </div>
            )}

            <form onSubmit={handleSubmit}>
              
              <div className="form-group">
                <label>Milestone Adı</label>
                <input 
                  type="text" 
                  name="milestoneName"
                  value={formData.milestoneName}
                  onChange={handleInputChange}
                  required 
                  placeholder="Örn: Tasarım Onayı" 
                />
              </div>

              <div className="form-group">
                <label>Planlanan Tarih</label>
                <input 
                  type="date" 
                  name="plannedDate"
                  value={formData.plannedDate}
                  onChange={handleInputChange}
                  required 
                />
              </div>

              <div className="form-group">
                <label>Gerçekleşen Tarih (Opsiyonel)</label>
                <input 
                  type="date" 
                  name="actualDate"
                  value={formData.actualDate}
                  onChange={handleInputChange}
                />
              </div>

              <div className="form-group">
                <label>Durum</label>
                <select 
                  name="status"
                  value={formData.status}
                  onChange={handleInputChange}
                  required
                >
                  <option value="Planlandı">Planlandı</option>
                  <option value="Devam Ediyor">Devam Ediyor</option>
                  <option value="Tamamlandı">Tamamlandı</option>
                </select>
              </div>

              <div className="modal-actions">
                <button 
                  type="button" 
                  className="btn-cancel" 
                  onClick={() => setIsModalOpen(false)}
                  disabled={loading}
                >
                  İptal
                </button>
                <button 
                  type="submit" 
                  className="btn-submit"
                  disabled={loading}
                >
                  {loading ? 'Kaydediliyor...' : (editingId ? 'Güncelle' : 'Kaydet')}
                </button>
              </div>

            </form>
          </div>
        </div>
      )}

    </div>
  )
}

export default MileStone