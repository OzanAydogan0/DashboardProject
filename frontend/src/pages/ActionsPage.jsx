import { useState, useEffect } from 'react';
import { projectService } from '../services/projectService';
import './ActionsPage.css';

function ActionsPage({ projectId }) {
  const [actions, setActions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchActions = async () => {
      try {
        setLoading(true);
        setError(null);

        let data = [];

        if (projectId) {
          data = await projectService.getProjectActions(projectId);
        } else {
          if (projectService.getAllActions) {
            data = await projectService.getAllActions();
          } else {
            const projects = await projectService.getProjects();
            const actionPromises = projects.map(p =>
              projectService.getProjectActions(p.projectId || p.id).catch(() => [])
            );
            const results = await Promise.all(actionPromises);
            data = results.flat();
          }
        }

        setActions(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Aksiyon verileri çekilirken hata oluştu:", err);
        setError(err.response?.data?.message || "Veritabanından aksiyon verileri çekilemedi.");
      } finally {
        setLoading(false);
      }
    };

    fetchActions();
  }, [projectId]);

  // Durum etiketi stili (Görseldeki "Devam Ediyor", "Planlandı", "Tamamlandı" uyumlu)
  const getStatusBadgeClass = (status) => {
    if (!status) return 'status-badge status-default';
    const val = status.toString().trim().toLowerCase();

    if (val.includes('devam') || val.includes('sürüyor')) return 'status-badge status-in-progress';
    if (val.includes('plan') || val.includes('bekliyor')) return 'status-badge status-planned';
    if (val.includes('tamam') || val.includes('kapalı')) return 'status-badge status-completed';
    if (val.includes('gecik') || val.includes('iptal')) return 'status-badge status-delayed';

    return 'status-badge status-default';
  };

  // Tarih Formatlayıcı (GG.AA.YYYY)
  const formatDate = (dateString) => {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return isNaN(date.getTime()) ? dateString : date.toLocaleDateString('tr-TR');
  };

  return (
    <div className="dashboard-card full-width">

      {loading ? (
        <div className="status-message">Veritabanından aksiyonlar yükleniyor...</div>
      ) : error ? (
        <div className="status-message error">{error}</div>
      ) : (
        <div className="table-responsive">
          <table className="action-table">
            <thead>
              <tr>
                <th>Aksiyon Tanımı</th>
                <th>Sorumlu</th>
                <th className="text-center">Hedef Tarih</th>
                <th className="text-center">Durum</th>
                <th>İlerleme</th>
              </tr>
            </thead>
            <tbody>
              {actions.map((action) => {
                const progressValue = action.actionProgress ?? action.ActionProgress ?? 0;
                return (
                  <tr key={action.actionId || action.ActionId}>
                    <td className="action-title-cell">
                      {action.actionDescription || action.ActionDescription}
                    </td>
                    <td className="action-owner-cell">
                      {action.actionOwnerUserFullName || action.ActionOwnerUserFullName || action.actionOwnerUserId || 'Atanmamış'}
                    </td>
                    <td className="text-center">
                      {formatDate(action.actionDueDate || action.ActionDueDate)}
                    </td>
                    <td className="text-center">
                      <span className={getStatusBadgeClass(action.actionStatus || action.ActionStatus)}>
                        {action.actionStatus || action.ActionStatus || 'Açık'}
                      </span>
                    </td>
                    <td className="progress-cell">
                      <div className="progress-wrapper">
                        <span className="progress-text">%{progressValue}</span>
                        <div className="progress-bar-bg">
                          <div
                            className="progress-bar-fill"
                            style={{ width: `${Math.min(100, Math.max(0, progressValue))}%` }}
                          ></div>
                        </div>
                      </div>
                    </td>
                  </tr>
                );
              })}

              {actions.length === 0 && (
                <tr>
                  <td colSpan="5" className="text-center status-message">
                    Veritabanında kayıtlı aksiyon bulunmamaktadır.
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

export default ActionsPage;