import React from 'react'
import './ProjectActionsPage.css'

/**
 * Aksiyon Kayıtları (Actions) Tablo Bileşeni
 * 
 * @param {Array} actions - Ana sayfadan gelen aksiyon verileri
 */
function ActionsPage({ actions = [] }) {

  // Tarih Biçimlendirme Yardımcı Fonksiyonu
  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card">
        <div className="card-header">
          <h2>Aksiyon Kayıtları</h2>
        </div>
        <div className="table-responsive">
          <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th>Aksiyon Tanımı</th>
                <th>Sorumlu</th>
                <th>Son Tarih</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {actions.length > 0 ? actions.map((a, idx) => (
                <tr key={a.id || idx}>
                  <td className="font-medium">{a.actionName || a.description || '-'}</td>
                  <td>{a.assignee || '-'}</td>
                  <td>{formatDate(a.dueDate)}</td>
                  <td>{a.status || '-'}</td>
                </tr>
              )) : (
                <tr>
                  <td colSpan="4" style={{ textAlign: 'center', padding: '20px' }}>
                    Kayıt bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

export default ActionsPage