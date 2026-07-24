import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { projectService } from '../services/projectService';
import './RisksPage.css';

function RisksPage({ projectId }) {
  const navigate = useNavigate();
  const [risks, setRisks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchRisks = async () => {
      try {
        setLoading(true);
        setError(null);

        let data = [];

        // EĞER Sayfaya tek bir projectId geldiyse o projeyi çeker, 
        // GELMEDİYSE (Genel Risk Sayfasıysa) tüm riskleri çeker.
        if (projectId) {
          data = await projectService.getProjectRisks(projectId);
        } else {
          // Backend'deki yeni GET /risks servisini çağırır
          if (projectService.getAllRisks) {
            data = await projectService.getAllRisks();
          } else {
            // Eğer servis henüz eklenmediyse mevcut projeleri tarayıp birleştirir (Fallback)
            const projects = await projectService.getProjects();
            const riskPromises = projects.map(p => 
              projectService.getProjectRisks(p.projectId || p.id).catch(() => [])
            );
            const results = await Promise.all(riskPromises);
            data = results.flat();
          }
        }

        setRisks(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Risk verileri çekilirken hata oluştu:", err);
        setError(err.response?.data?.message || "Veritabanından risk verileri alınamadı.");
      } finally {
        setLoading(false);
      }
    };

    fetchRisks();
  }, [projectId]);

  // Veritabanından "Kırmızı", "Sarı", "Yeşil" veya "Kritik", "Orta", "Düşük" gelse de hepsini yakalar
  const getBadgeClass = (healthLevel) => {
    if (!healthLevel) return 'badge-default';
    
    const val = healthLevel.toString().trim().toLowerCase();

    // Kırmızı / Kritik
    if (val.includes('kırmızı') || val.includes('kirmizi') || val.includes('kritik') || val.includes('red')) {
      return 'badge-critical';
    }
    // Turuncu / Yüksek
    if (val.includes('turuncu') || val.includes('yüksek') || val.includes('yuksek') || val.includes('orange')) {
      return 'badge-high';
    }
    // Sarı / Orta
    if (val.includes('sarı') || val.includes('sari') || val.includes('orta') || val.includes('yellow')) {
      return 'badge-medium';
    }
    // Yeşil / Düşük
    if (val.includes('yeşil') || val.includes('yesil') || val.includes('düşük') || val.includes('dusuk') || val.includes('green')) {
      return 'badge-low';
    }

    return 'badge-default';
  };

  return (
    <div className="dashboard-card full-width">

      {loading ? (
        <div className="status-message">Veritabanından risk verileri yükleniyor...</div>
      ) : error ? (
        <div className="status-message error">{error}</div>
      ) : (
        <div className="table-responsive">
          <table className="risk-table">
            <thead>
              <tr>
                {!projectId && <th>Proje Adı</th>}
                <th>Risk Başlığı</th>
                <th className="text-center">Risk Puanı</th>
                <th className="text-center">Olasılık</th>
                <th className="text-center">Etki</th>
                <th className="text-center">Seviye</th>
                <th>Sahip</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {risks.map((risk) => (
                <tr
                  key={risk.riskId}
                  onClick={() => navigate(`/projects/${risk.projectId || risk.ProjectId}`)}
                  style={{ cursor: 'pointer' }}
                >
                  {/* Proje Adı (Sadece Genel Sayfada Görünür) */}
                  {!projectId && (
                    <td style={{ fontWeight: '600', color: '#475569' }}>
                      {risk.projectName || risk.projectCode || '-'}
                    </td>
                  )}
                  <td className="risk-title-cell">{risk.riskTitle}</td>
                  <td className="text-center">{risk.riskScore ?? 0}</td>
                  <td className="text-center">{risk.riskProbability ?? 0}</td>
                  <td className="text-center">{risk.riskImpact ?? 0}</td>
                  <td className="text-center">
                    <span className={`risk-badge ${getBadgeClass(risk.riskHealth)}`}>
                      {risk.riskHealth || 'Belirsiz'}
                    </span>
                  </td>
                  <td>{risk.riskOwnerFullName || 'Atanmadı'}</td>
                  <td>{risk.riskStatus || 'Açık'}</td>
                </tr>
              ))}

              {risks.length === 0 && (
                <tr>
                  <td colSpan={!projectId ? 8 : 7} className="text-center status-message">
                    Veritabanında kayıtlı risk bulunmamaktadır.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default RisksPage;