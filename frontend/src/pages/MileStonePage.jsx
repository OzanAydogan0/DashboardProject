import React, { useState } from 'react'
import './MileStonePage.css'

/**
 * Kilometre Taşları (Milestones) Bileşeni - Ekleme ve Düzenleme Destekli
 * 
 * @param {Array} milestones - Veritabanından gelen mevcut kilometre taşları
 * @param {String|Number} projectId - İlişkili projenin ID değeri
 * @param {Function} onMilestoneUpdated - Veri eklendiğinde/güncellendiğinde ana sayfayı tazeleyen callback
 */
function MileStone({ milestones = [], projectId, onMilestoneUpdated }) {
  // Modal görünürlük state'i
  const [isModalOpen, setIsModalOpen] = useState(false)
  
  // O an düzenlenmekte olan Kayıt ID'si (null ise Yeni Ekleme modundadır)
  const [editingId, setEditingId] = useState(null)
  
  // Yüklenme durumu
  const [loading, setLoading] = useState(false)

  // Form girdilerini tutan State
  const [formData, setFormData] = useState({
    milestoneName: '',
    plannedDate: '',
    actualDate: '',
    status: 'Planlandı'
  })

  // Input değişikliklerini yakalama
  const handleInputChange = (e) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
  }

  // Tarih Biçimlendirme (YYYY-MM-DD -> TR Formatı)
  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  // Date inputları için YYYY-MM-DD formatına dönüştürme yardımcı fonksiyonu
  const formatForInput = (dateString) => {
    if (!dateString) return ''
    return new Date(dateString).toISOString().split('T')[0]
  }

  // Durum Renk Sınıfı
  const getMilestoneStatusStyle = (status) => {
    const s = (status || '').toLowerCase()
    if (s.includes('tamamlandı')) return 'status-completed'
    if (s.includes('devam')) return 'status-inprogress'
    return 'status-planned'
  }

  // --- MODAL AÇMA FONKSİYONLARI ---
  
  // 1. Yeni Ekleme Modunda Modalı Aç
  const handleOpenAddModal = () => {
    setEditingId(null)
    setFormData({
      milestoneName: '',
      plannedDate: '',
      actualDate: '',
      status: 'Planlandı'
    })
    setIsModalOpen(true)
  }

  // 2. Düzenleme Modunda Modalı Aç (Mevcut verileri forma doldurur)
  const handleOpenEditModal = (item) => {
    setEditingId(item.id)
    setFormData({
      milestoneName: item.milestoneName || '',
      plannedDate: formatForInput(item.plannedDate),
      actualDate: formatForInput(item.actualDate),
      status: item.milestoneStatus || item.status || 'Planlandı'
    })
    setIsModalOpen(true)
  }

  // --- VERİTABANINA KAYIT/GÜNCELLEME İŞLEMİ (POST / PUT) ---
  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)

    // Düzenleme mi yoksa Yeni Ekleme mi yapılıyor?
    const isEdit = editingId !== null
    const url = isEdit 
      ? `/api/milestones/${editingId}` // Güncelleme adresi (PUT)
      : `/api/projects/${projectId}/milestones` // Yeni kayıt adresi (POST)
    
    const method = isEdit ? 'PUT' : 'POST'

    try {
      const response = await fetch(url, {
        method: method,
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          projectId: projectId,
          milestoneName: formData.milestoneName,
          plannedDate: formData.plannedDate,
          actualDate: formData.actualDate || null,
          status: formData.status
        })
      })

      if (response.ok) {
        setIsModalOpen(false)
        // Ana sayfaya haber ver, veritabanından en güncel halini çeksin
        if (onMilestoneUpdated) {
          onMilestoneUpdated()
        }
      } else {
        alert('İşlem sırasında bir hata oluştu.')
      }
    } catch (error) {
      console.error('API Hatası:', error)
      alert('Sunucuya bağlanılamadı.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card timeline-card">
        
        {/* Üst Kısım / Ekle Butonu */}
        <div className="timeline-header-actions">
          <div style={{ flex: 1 }}></div>
          <button 
            className="btn-add-milestone" 
            onClick={handleOpenAddModal}>
            + Yeni Milestone
          </button>
        </div>

        {/* Tablo */}
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
                  <tr key={m.id || idx}>
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
                    {/* YENİ: DÜZENLE BUTONU SÜTUNU */}
                    <td style={{ textAlign: 'right', paddingRight: '16px' }}>
                      <button 
                        className="btn-action-edit"
                        onClick={() => handleOpenEditModal(m)}
                        title="Düzenle"
                      >
                        Düzenle
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

      {/* --- FORM MODAL (EKLEME VE DÜZENLEME) --- */}
      {isModalOpen && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>{editingId ? 'Milestone Düzenle' : 'Yeni Kilometre Taşı Ekle'}</h3>
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
                  {loading ? 'Kaydediliyor...' : (editingId ? 'Güncelle' : 'Veritabanına Kaydet')}
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