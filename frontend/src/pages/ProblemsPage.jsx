import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import Pagination from '../components/Pagination';
import { projectService } from '../services/projectService';
import { useAlert } from '../components/AlertProvider';
import { usePagination } from '../utils/usePagination';
import './ProblemsPage.css';

const getCurrentUserId = () => {
    try {
        const userString = localStorage.getItem('user');
        const user = userString ? JSON.parse(userString) : null;
        return user?.userId || user?.UserId || user?.id || '';
    } catch {
        return '';
    }
};

const emptyForm = {
    issueTitle: '',
    issuePriority: 'Orta',
    issueOwnerUserId: getCurrentUserId(),
    issueDueDate: '',
    issueStatus: 'Açık',
    issueImpact: 'Orta',
    rootCause: '',
    issueResolution: ''
};

function ProblemsPage() {
    const { id: projectId } = useParams();
    const { addAlert } = useAlert();

    const [issues, setIssues] = useState([]);
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [form, setForm] = useState(emptyForm);
    const [editingIssueId, setEditingIssueId] = useState(null);
    const [showIssueModal, setShowIssueModal] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isDeletingId, setIsDeletingId] = useState(null);
    const [priorityFilter, setPriorityFilter] = useState('Hepsi');
    const [canWrite, setCanWrite] = useState(false);

    const formatDate = (dateString) => {
        if (!dateString) return '-';
        return new Date(dateString).toLocaleDateString('tr-TR');
    };

    const getPriorityStyle = (priority) => {
        switch (priority?.toLowerCase()) {
            case 'kritik':
            case 'critical':
                return { bg: '#f3e5d1', text: '#7c4a1b' };
            case 'yüksek':
            case 'high':
                return { bg: '#fee2e2', text: '#991b1b' };
            case 'orta':
            case 'medium':
                return { bg: '#fef3c7', text: '#92400e' };
            case 'düşük':
            case 'low':
                return { bg: '#d1fae5', text: '#065f46' };
            default:
                return { bg: '#f3f4f6', text: '#374151' };
        }
    };

    const fetchIssues = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await projectService.getProjectIssues(projectId);
            setIssues(Array.isArray(data) ? data : []);
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Sorun kayıtları yüklenirken bir hata oluştu.';
            setError(backendMessage);
        } finally {
            setLoading(false);
        }
    };

    const fetchUsers = async () => {
        try {
            const userData = await projectService.getUsers();
            setUsers(Array.isArray(userData) ? userData : []);
        } catch (err) {
            console.error('Kullanıcı listesi alınamadı:', err);
            setUsers([]);
        }
    };

    useEffect(() => {
        const userString = localStorage.getItem('user');
        try {
            const user = userString ? JSON.parse(userString) : null;
            const userRole = user?.userRole || user?.UserRole || user?.role || user?.Role || '';
            const isExecutive = ['Üst Yönetim İzleyicisi', 'Üst Yönetim'].includes(userRole);
            setCanWrite(!isExecutive);
        } catch {
            setCanWrite(false);
        }

        if (projectId) {
            fetchIssues();
            fetchUsers();
        }
    }, [projectId]);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setForm((prev) => ({ ...prev, [name]: value }));
    };

    const filteredIssues = priorityFilter === 'Hepsi'
        ? issues
        : issues.filter((issue) => (issue.issuePriority || '').toLowerCase() === priorityFilter.toLowerCase());
    const issuePagination = usePagination(filteredIssues);

    const resetForm = () => {
        setEditingIssueId(null);
        setForm({
            ...emptyForm,
            issueOwnerUserId: getCurrentUserId()
        });
    };

    const openCreateModal = () => {
        resetForm();
        setShowIssueModal(true);
    };

    const closeIssueModal = () => {
        setShowIssueModal(false);
        resetForm();
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!projectId) return;

        setIsSubmitting(true);
        setError(null);

        try {
            const payload = {
                projectId,
                issueTitle: form.issueTitle,
                issuePriority: form.issuePriority,
                issueOwnerUserId: form.issueOwnerUserId || getCurrentUserId(),
                issueDueDate: form.issueDueDate ? new Date(form.issueDueDate).toISOString() : null,
                issueStatus: form.issueStatus,
                issueImpact: form.issueImpact,
                rootCause: form.rootCause || '',
                issueResolution: form.issueResolution || ''
            };

            if (editingIssueId) {
                await projectService.updateIssue(editingIssueId, payload);
            } else {
                await projectService.createProjectIssue(projectId, payload);
            }

            closeIssueModal();
            await fetchIssues();
            addAlert(editingIssueId ? 'Sorun kaydı güncellendi.' : 'Yeni sorun kaydı eklendi.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Sorun kaydı işlenemedi.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleEdit = (issue) => {
        setEditingIssueId(issue.issueId);
        setForm({
            issueTitle: issue.issueTitle || '',
            issuePriority: issue.issuePriority || 'Orta',
            issueOwnerUserId: issue.issueOwnerUserId || getCurrentUserId(),
            issueDueDate: issue.issueDueDate ? new Date(issue.issueDueDate).toISOString().slice(0, 10) : '',
            issueStatus: issue.issueStatus || 'Açık',
            issueImpact: issue.issueImpact || 'Orta',
            rootCause: issue.rootCause || '',
            issueResolution: issue.issueResolution || ''
        });
        setShowIssueModal(true);
    };

    const handleSoftDelete = async (issue) => {
        if (!window.confirm('Bu sorunu soft delete yapmak istediğinize emin misiniz? Durum alanı "Kapalı" olarak güncellenecek.')) {
            return;
        }

        setIsDeletingId(issue.issueId);
        setError(null);

        try {
            await projectService.updateIssue(issue.issueId, {
                issueStatus: 'Kapalı',
                issueResolution: 'Soft delete ile kapatıldı',
                issueTitle: issue.issueTitle || '',
                issuePriority: issue.issuePriority || 'Orta',
                issueImpact: issue.issueImpact || 'Orta'
            });
            await fetchIssues();
            addAlert('Sorun başarıyla kapatıldı.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Sorun kapatılamadı.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsDeletingId(null);
        }
    };

    if (loading) return <div className="tab-content-wrapper fade-in"><p className="status-text">Sorunlar ve çözüm detayları yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p className="status-text error-text">Hata: {error}</p></div>;

    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card problems-card">
                <div className="card-header problems-header">
                    <h2>Sorun (Issue) Kayıtları</h2>
                    {canWrite && (
                        <button type="button" className="btn-primary" onClick={openCreateModal}>
                            Yeni Sorun
                        </button>
                    )}
                </div>

                {!canWrite && (
                    <div className="permission-note">Bu projede sorun ekleme/düzenleme yetkiniz bulunmuyor.</div>
                )}

                <div className="problems-filter-bar">
                    <label className="problems-filter-item">
                        <span>Öncelik Filtre</span>
                        <select
                            value={priorityFilter}
                            onChange={(e) => {
                                setPriorityFilter(e.target.value);
                                issuePagination.setCurrentPage(1);
                            }}
                        >
                            <option value="Hepsi">Hepsi</option>
                            <option value="Düşük">Düşük</option>
                            <option value="Orta">Orta</option>
                            <option value="Yüksek">Yüksek</option>
                            <option value="Kritik">Kritik</option>
                        </select>
                    </label>
                </div>

                {showIssueModal && canWrite && (
                    <div className="issue-modal-overlay" onClick={closeIssueModal}>
                        <div className="issue-modal" onClick={(e) => e.stopPropagation()}>
                            <div className="issue-modal-header">
                                <h3>{editingIssueId ? 'Sorun Düzenle' : 'Yeni Sorun Ekle'}</h3>
                                <button type="button" className="modal-close-btn" onClick={closeIssueModal}>×</button>
                            </div>

                            <form className="issue-form" onSubmit={handleSubmit}>
                                <div className="form-grid">
                                    <label>
                                        <span>Sorun Tanımı</span>
                                        <input name="issueTitle" value={form.issueTitle} onChange={handleInputChange} required />
                                    </label>
                                    <label>
                                        <span>Öncelik</span>
                                        <select name="issuePriority" value={form.issuePriority} onChange={handleInputChange}>
                                            <option value="Düşük">Düşük</option>
                                            <option value="Orta">Orta</option>
                                            <option value="Yüksek">Yüksek</option>
                                            <option value="Kritik">Kritik</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Etki</span>
                                        <select name="issueImpact" value={form.issueImpact} onChange={handleInputChange}>
                                            <option value="Yüksek">Yüksek</option>
                                            <option value="Orta">Orta</option>
                                            <option value="Düşük">Düşük</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Durum</span>
                                        <select name="issueStatus" value={form.issueStatus} onChange={handleInputChange}>
                                            <option value="Açık">Açık</option>
                                            <option value="İzleniyor">İzleniyor</option>
                                            <option value="Kapalı">Kapalı</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Hedef Tarihi</span>
                                        <input type="date" name="issueDueDate" value={form.issueDueDate} onChange={handleInputChange} />
                                    </label>
                                    <label>
                                        <span>Sorumlu Kullanıcı</span>
                                        <select name="issueOwnerUserId" value={form.issueOwnerUserId} onChange={handleInputChange} required>
                                            <option value="">-- Proje Yöneticisi Seçiniz --</option>
                                            {users.map((user) => {
                                                const userId = user.userId || user.UserId || user.id;
                                                const userName = user.fullName || user.FullName || user.userName || user.UserName || user.name || userId;
                                                return (
                                                    <option key={userId} value={userId}>
                                                        {userName}
                                                    </option>
                                                );
                                            })}
                                        </select>
                                    </label>
                                    <label className="full-width">
                                        <span>Kök Neden</span>
                                        <textarea name="rootCause" value={form.rootCause} onChange={handleInputChange} rows="3" />
                                    </label>
                                    <label className="full-width">
                                        <span>Çözüm</span>
                                        <textarea name="issueResolution" value={form.issueResolution} onChange={handleInputChange} rows="3" placeholder="Çözüm açıklamasını buraya yazabilirsiniz" />
                                    </label>
                                </div>

                                <div className="form-actions">
                                    <button type="submit" className="btn-primary" disabled={isSubmitting}>
                                        {isSubmitting ? 'Kaydediliyor...' : editingIssueId ? 'Güncelle' : 'Ekle'}
                                    </button>
                                    <button type="button" className="btn-secondary" onClick={closeIssueModal}>
                                        İptal
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}

                <div className="table-responsive">
                    {filteredIssues.length === 0 ? (
                        <p className="empty-state">Bu projeye ait henüz bir sorun kaydı bulunmuyor.</p>
                    ) : (
                        <table className="modern-table problems-table">
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Sorun Tanımı</th>
                                    <th>Öncelik</th>
                                    <th>Etki</th>
                                    <th>Durum</th>
                                    <th>Sorumlu</th>
                                    <th>Kök Neden & Çözüm</th>
                                    <th>Tarih Detayları</th>
                                    {canWrite && <th>İşlem</th>}
                                </tr>
                            </thead>
                            <tbody>
                                {issuePagination.paginatedItems.map((i) => {
                                    const priorityBadge = getPriorityStyle(i.issuePriority);

                                    return (
                                        <tr key={i.issueId}>
                                            <td><strong>{i.issueId}</strong></td>
                                            <td><div className="font-medium">{i.issueTitle || '-'}</div></td>
                                            <td>
                                                <span className="priority-badge" style={{ backgroundColor: priorityBadge.bg, color: priorityBadge.text }}>
                                                    {i.issuePriority || 'Belirtilmemiş'}
                                                </span>
                                            </td>
                                            <td><strong>{i.issueImpact || '-'}</strong></td>
                                            <td>{i.issueStatus || '-'}</td>
                                            <td>
                                                <div>{i.issueOwnerFullName || i.issueOwnerUser?.fullName || 'Atanmamış'}</div>
                                                {i.issueOwnerUserId && (
                                                    <small className="owner-id-text">User ID: {i.issueOwnerUserId}</small>
                                                )}
                                            </td>
                                            <td>
                                                <div className="issue-detail-line"><strong>Kök Neden:</strong> {i.rootCause || '-'}</div>
                                                <div className="issue-detail-line issue-solution"><strong>Çözüm:</strong> {i.issueResolution || '-'}</div>
                                            </td>
                                            <td>
                                                <div className="date-detail-list">
                                                    <div><strong>Açılış:</strong> {formatDate(i.openedDate)}</div>
                                                    <div><strong>Hedef:</strong> {formatDate(i.issueDueDate)}</div>
                                                    <div><strong>Kapanış:</strong> {formatDate(i.closedDate)}</div>
                                                </div>
                                            </td>
                                            {canWrite && (
                                                <td>
                                                    <div className="row-actions">
                                                        <button type="button" className="btn-secondary" onClick={() => handleEdit(i)}>
                                                            Düzenle
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="btn-danger"
                                                            onClick={() => handleSoftDelete(i)}
                                                            disabled={isDeletingId === i.issueId}
                                                        >
                                                            {isDeletingId === i.issueId ? 'Kapatılıyor...' : 'Sil'}
                                                        </button>
                                                    </div>
                                                </td>
                                            )}
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    )}
                    <Pagination
                        currentPage={issuePagination.currentPage}
                        itemLabel="sorun"
                        onPageChange={issuePagination.setCurrentPage}
                        totalItems={issuePagination.totalItems}
                        totalPages={issuePagination.totalPages}
                    />
                </div>
            </div>
        </div>
    );
}

export default ProblemsPage;
