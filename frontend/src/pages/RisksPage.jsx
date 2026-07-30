import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from '../router';
import { projectService } from '../services/projectService';
import { useAlert } from '../components/alertContext';
import { HEALTH_STATUS, normalizeHealthStatus } from '../utils/healthStatus';
import './RisksPage.css';

function RisksPage({ projectId }) {
  const navigate = useNavigate();
  const { addAlert } = useAlert();
  const [risks, setRisks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

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

        setCurrentPage(1);
        setRisks(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Risk verileri çekilirken hata oluştu:", err);
        const message = err.response?.data?.message || "Veritabanından risk verileri alınamadı.";
        setError(message);
        addAlert(message, 'error');
      } finally {
        setLoading(false);
      }
    };

    fetchRisks();
  }, [projectId, addAlert]);

  const getBadgeClass = (healthLevel) => {
    const normalizedHealth = normalizeHealthStatus(healthLevel);
    if (normalizedHealth === HEALTH_STATUS.CRITICAL) return 'badge-critical';
    if (normalizedHealth === HEALTH_STATUS.MEDIUM) return 'badge-medium';
    if (normalizedHealth === HEALTH_STATUS.GOOD) return 'badge-low';
    return 'badge-default';
  };

  const sortedRisks = useMemo(() => {
    const list = Array.isArray(risks) ? [...risks] : [];

    return list.sort((a, b) => {
      const scoreA = Number(a.riskScore ?? a.RiskScore ?? 0) || 0;
      const scoreB = Number(b.riskScore ?? b.RiskScore ?? 0) || 0;
      if (scoreA !== scoreB) return scoreB - scoreA;

      const dueA = a.riskDueDate || a.RiskDueDate || '';
      const dueB = b.riskDueDate || b.RiskDueDate || '';
      if (dueA && dueB) {
        const dateA = new Date(dueA).getTime();
        const dateB = new Date(dueB).getTime();
        if (!Number.isNaN(dateA) && !Number.isNaN(dateB) && dateA !== dateB) {
          return dateA - dateB;
        }
      }

      const titleA = (a.riskTitle || a.RiskTitle || '').toString();
      const titleB = (b.riskTitle || b.RiskTitle || '').toString();
      return titleA.localeCompare(titleB, 'tr');
    });
  }, [risks]);

  const totalPages = Math.max(1, Math.ceil(sortedRisks.length / pageSize));
  const paginatedRisks = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    return sortedRisks.slice(startIndex, startIndex + pageSize);
  }, [sortedRisks, currentPage]);

  return (
    <div className="dashboard-card full-width">

      {loading ? (
        <div className="status-message">Veritabanından risk verileri yükleniyor...</div>
      ) : error ? (
        <div className="status-message error">{error}</div>
      ) : (
        <>
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
                <th>Azaltım / Müdahale</th>
                <th>Sahip</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {paginatedRisks.map((risk) => (
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
                      {normalizeHealthStatus(risk.riskHealth)}
                    </span>
                  </td>
                  <td className="mitigation-cell">{risk.riskMitigation || '-'}</td>
                  <td>{risk.riskOwnerFullName || 'Atanmadı'}</td>
                  <td>{risk.riskStatus || 'Açık'}</td>
                </tr>
              ))}

              {sortedRisks.length === 0 && (
                <tr>
                  <td colSpan={!projectId ? 9 : 8} className="text-center status-message">
                    Veritabanında kayıtlı risk bulunmamaktadır.
                  </td>
                </tr>
              )}
            </tbody>
            </table>

            <div className="pagination-bar">
              <span className="pagination-summary">
                Toplam {sortedRisks.length} risk • Sayfa {currentPage} / {totalPages}
              </span>
              <div className="pagination-controls">
                <button
                  type="button"
                  className="pagination-btn"
                  onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
                  disabled={currentPage === 1}
                >
                  Önceki
                </button>
                {Array.from({ length: totalPages }, (_, index) => index + 1).map((pageNumber) => (
                  <button
                    key={pageNumber}
                    type="button"
                    className={`pagination-page-btn ${currentPage === pageNumber ? 'active' : ''}`}
                    onClick={() => setCurrentPage(pageNumber)}
                  >
                    {pageNumber}
                  </button>
                ))}
                <button
                  type="button"
                  className="pagination-btn"
                  onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
                  disabled={currentPage === totalPages}
                >
                  Sonraki
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default RisksPage;
