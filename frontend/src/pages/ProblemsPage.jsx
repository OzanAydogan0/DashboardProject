import React from 'react'

/**
 * Sorun / Problem Kayıtları (Problems / Issues) Tablo Bileşeni
 * 
 * @param {Array} issues - Ana sayfadan (ProjectDetailPage) gelen sorun verileri dizisi
 */
function ProblemsPage({ issues = [] }) {

  // Tarih Biçimlendirme Yardımcı Fonksiyonu
  const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('tr-TR')
  }

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card">
        
        {/* Kart Başlığı */}
        <div className="card-header">
          <h2>Sorun (Issue) Kayıtları</h2>
        </div>

        {/* Sorunlar Tablosu */}
        <div className="table-responsive">
          <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th>Sorun Tanımı</th>
                <th>Öncelik</th>
                <th>Sorumlu</th>
                <th>Oluşturulma Tarihi</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {issues.length > 0 ? (
                issues.map((i, idx) => (
                  <tr key={i.id || idx}>
                    {/* Sorun Adı veya Açıklaması */}
                    <td className="font-medium">{i.issueName || i.description || '-'}</td>
                    
                    {/* Öncelik Düzeyi */}
                    <td>{i.priority || '-'}</td>
                    
                    {/* Atanan Sorumlu */}
                    <td>{i.owner || i.assignee || '-'}</td>
                    
                    {/* Oluşturulma / Kayıt Tarihi */}
                    <td>{formatDate(i.createdAt || i.date)}</td>
                    
                    {/* Sorun Durumu */}
                    <td>{i.status || '-'}</td>
                  </tr>
                ))
              ) : (
                /* Veri Bulunamadığında Gösterilecek Satır */
                <tr>
                  <td colSpan="5" style={{ textAlign: 'center', padding: '20px' }}>
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

export default ProblemsPage