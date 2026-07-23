import React from 'react'

/**
 * Geçmiş Dönem Raporları Tablo Bileşeni
 * 
 * @param {Array} reports - Ana sayfadan gelen rapor verileri
 */
function ReportsPage({ reports = [] }) {

  // Tarih Biçimlendirme Yardımcı Fonksiyonu
  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card">
        <div className="card-header">
          <h2>Geçmiş Dönem Raporları</h2>
        </div>
        <div className="table-responsive">
          <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th>Dönem</th>
                <th>Oluşturulma Tarihi</th>
                <th>Durum</th>
                <th>Özet</th>
              </tr>
            </thead>
            <tbody>
              {reports.length > 0 ? reports.map((r, idx) => (
                <tr key={r.id || idx}>
                  <td className="font-medium">{r.period || '-'}</td>
                  <td>{formatDate(r.createdAt || r.reportDate)}</td>
                  <td>{r.status || '-'}</td>
                  <td 
                    style={{ maxWidth: '300px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }} 
                    title={r.executiveSummary}
                  >
                    {r.executiveSummary || '-'}
                  </td>
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

export default ReportsPage