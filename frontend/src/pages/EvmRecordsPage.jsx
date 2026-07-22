import React from 'react'

/**
 * EVM Verileri Tablo Bileşeni
 * 
 * @param {Array} evmRecords - Ana sayfadan gelen EVM kayıtları
 * @param {String} currency - Projenin para birimi (Örn: 'TRY', 'USD')
 */
function EvmRecordsPage({ evmRecords = [], currency = 'TRY' }) {

  // Para Birimi Biçimlendirme
  const formatCurrency = (val, curr) => {
    if (val == null) return '-'
    return new Intl.NumberFormat('tr-TR', { 
      style: 'currency', 
      currency: curr, 
      maximumFractionDigits: 0 
    }).format(val)
  }

  // SPI ve CPI Değerleri İçin Renklendirme Sınıfı
  const getSpiCpiClass = (val) => {
    if (val >= 0.95) return 'text-success' 
    if (val >= 0.85) return 'text-warning' 
    return 'text-danger'                   
  }

  return (
    <div className="tab-content-wrapper fade-in">
      <div className="dashboard-card shadow-card">
        <div className="card-header">
          <h2>Tarihsel Kazanılmış Değer (EVM) Verileri</h2>
        </div>
        <div className="table-responsive">
          <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
            <thead>
              <tr>
                <th>Dönem</th>
                <th>PV (Planlanan)</th>
                <th>EV (Kazanılan)</th>
                <th>AC (Gerçekleşen)</th>
                <th>SPI</th>
                <th>CPI</th>
              </tr>
            </thead>
            <tbody>
              {evmRecords.length > 0 ? evmRecords.map((e, idx) => (
                <tr key={e.id || idx}>
                  <td className="font-medium">{e.period || '-'}</td>
                  <td>{formatCurrency(e.pv, currency)}</td>
                  <td>{formatCurrency(e.ev, currency)}</td>
                  <td>{formatCurrency(e.ac, currency)}</td>
                  <td className={`font-medium ${getSpiCpiClass(e.spi)}`}>{e.spi || '-'}</td>
                  <td className={`font-medium ${getSpiCpiClass(e.cpi)}`}>{e.cpi || '-'}</td>
                </tr>
              )) : (
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
    </div>
  )
}

export default EvmRecordsPage