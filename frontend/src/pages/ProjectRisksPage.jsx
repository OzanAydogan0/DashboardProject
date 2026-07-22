import React from 'react'

/**
 * Risk Kayıtları (Project Risks) Tablo Bileşeni
 * 
 * @param {Array} risks - Ana sayfadan (ProjectDetailPage) gelen risk verileri dizisi
 */
function ProjectRisksPage({ risks = [] }) {
  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card">
        
        {/* Kart Başlığı */}
        <div className="card-header">
          <h2>Risk Kayıtları</h2>
        </div>

        {/* Risk Tablosu */}
        <div className="table-responsive">
          <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th>Risk Tanımı</th>
                <th>Olasılık</th>
                <th>Etki</th>
                <th>Risk Puanı</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {risks.length > 0 ? (
                risks.map((r, idx) => {
                  // Olasılık ve etki değerleri mevcutsa puanı hesapla
                  const riskScore = (r.probability && r.impact) ? (r.probability * r.impact) : '-'

                  return (
                    <tr key={r.id || idx}>
                      {/* Risk Adı veya Açıklaması */}
                      <td className="font-medium">{r.riskName || r.description || '-'}</td>
                      
                      {/* Olasılık ve Etki Değerleri */}
                      <td>{r.probability ?? '-'}</td>
                      <td>{r.impact ?? '-'}</td>
                      
                      {/* Risk Puanı (Olasılık x Etki) */}
                      <td>{riskScore}</td>
                      
                      {/* Risk Durumu */}
                      <td>{r.status || '-'}</td>
                    </tr>
                  )
                })
              ) : (
                /* Veri Olmadığında Gösterilecek Alan */
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

export default ProjectRisksPage