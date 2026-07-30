import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { projectService } from '../services/projectService';
import { useAlert } from '../components/AlertProvider';
import './ActionsPage.css';

function ActionsPage({ projectId }) {
  const navigate = useNavigate();
  const { addAlert } = useAlert();
  const [actions, setActions] = useState([]);
  const [projectNameMap, setProjectNameMap] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  useEffect(() => {
    const fetchProjectNames = async () => {
      try {
        const projects = await projectService.getProjects();
        const map = {};

        (Array.isArray(projects) ? projects : []).forEach((project) => {
          const projectIdValue = project.projectId || project.id || project.ProjectId;
          if (projectIdValue) {
            map[projectIdValue] = project.projectName || project.ProjectName || project.name || '-';
          }
        });

        setProjectNameMap(map);
      } catch (err) {
        console.error('Proje adları yüklenirken hata oluştu:', err);
      }
    };

    fetchProjectNames();
  }, []);

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

        setCurrentPage(1);
        setActions(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Aksiyon verileri çekilirken hata oluştu:", err);
        const message = err.response?.data?.message || "Veritabanından aksiyon verileri çekilemedi.";
        setError(message);
        addAlert(message, 'error');
      } finally {
        setLoading(false);
      }
    };

    fetchActions();
  }, [projectId, addAlert]);

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

  const getProjectName = (action) => {
    const directName = action.projectName || action.ProjectName || action.project?.projectName || action.project?.ProjectName;
    if (directName) return directName;

    const projectIdValue = action.projectId || action.ProjectId || action.project?.projectId || action.project?.ProjectId;
    return projectIdValue ? projectNameMap[projectIdValue] || '-' : '-';
  };

  const sortedActions = useMemo(() => {
    const list = Array.isArray(actions) ? [...actions] : [];

    return list.sort((a, b) => {
      const aDate = a.actionDueDate || a.ActionDueDate || '';
      const bDate = b.actionDueDate || b.ActionDueDate || '';
      const aHasDate = Boolean(aDate);
      const bHasDate = Boolean(bDate);

      if (aHasDate && bHasDate) {
        const aTime = new Date(aDate).getTime();
        const bTime = new Date(bDate).getTime();
        if (!Number.isNaN(aTime) && !Number.isNaN(bTime) && aTime !== bTime) {
          return aTime - bTime;
        }
      } else if (aHasDate !== bHasDate) {
        return aHasDate ? -1 : 1;
      }

      const getStatusWeight = (status = '') => {
        const value = status.toString().trim().toLowerCase();
        if (value.includes('iptal') || value.includes('gecik')) return 0;
        if (value.includes('devam') || value.includes('sürüyor')) return 1;
        if (value.includes('plan') || value.includes('bekliyor')) return 2;
        if (value.includes('tamam') || value.includes('kapalı')) return 3;
        return 4;
      };

      const weightDiff = getStatusWeight(a.actionStatus || a.ActionStatus) - getStatusWeight(b.actionStatus || b.ActionStatus);
      if (weightDiff !== 0) return weightDiff;

      const aText = (a.actionDescription || a.ActionDescription || '').toString();
      const bText = (b.actionDescription || b.ActionDescription || '').toString();
      return aText.localeCompare(bText, 'tr');
    });
  }, [actions]);

  const totalPages = Math.max(1, Math.ceil(sortedActions.length / pageSize));
  const paginatedActions = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    return sortedActions.slice(startIndex, startIndex + pageSize);
  }, [sortedActions, currentPage]);

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
                <th>Proje</th>
                <th>Bağlı Risk</th>
                <th>Bağlı Sorun</th>
                <th>Sorumlu</th>
                <th className="text-center">Hedef Tarih</th>
                <th className="text-center">Durum</th>
                <th>İlerleme</th>
              </tr>
            </thead>
            <tbody>
              {paginatedActions.map((action) => {
                const progressValue = action.actionProgress ?? action.ActionProgress ?? 0;
                const projectIdFromAction = action.projectId || action.ProjectId;
                return (
                  <tr
                    key={action.actionId || action.ActionId}
                    onClick={() => navigate(`/projects/${projectIdFromAction}`)}
                    style={{ cursor: 'pointer' }}
                  >
                    <td className="action-title-cell">
                      {action.actionDescription || action.ActionDescription}
                    </td>
                    <td className="action-owner-cell">
                      {getProjectName(action)}
                    </td>
                    <td className="action-owner-cell">
                      {action.riskTitle || action.RiskTitle || '-'}
                    </td>
                    <td className="action-owner-cell">
                      <div>{action.issueTitle || action.IssueTitle || '-'}</div>
                      {(action.issueId || action.IssueId) && (
                        <small className="linked-record-id">Sorun ID: {action.issueId || action.IssueId}</small>
                      )}
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

              {sortedActions.length === 0 && (
                <tr>
                  <td colSpan="8" className="text-center status-message">
                    Veritabanında kayıtlı aksiyon bulunmamaktadır.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <div className="pagination-bar">
            <span className="pagination-summary">
              Toplam {sortedActions.length} aksiyon • Sayfa {currentPage} / {totalPages}
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
      )}
    </div>
  );
}

export default ActionsPage;
