import { useState, useEffect, useEffectEvent } from 'react';
import { useParams } from 'react-router-dom';
import Pagination from '../components/Pagination';
import { projectService } from '../services/projectService';
import { useAlert } from '../components/AlertProvider';
import {
    canWriteProject,
    getAssignableProjectUsers,
    getDefaultProjectAssigneeId,
    getUserRecordId,
    getValidProjectAssigneeId,
} from '../utils/permissionHelper';
import { usePagination } from '../utils/usePagination';
import './ProjectActionsPage.css';

const SOURCE_TYPES = ['Risk', 'Sorun', 'Kilometre Taşı', 'PIR', 'Yönetim Kararı', 'Diğer'];

const emptyForm = {
    actionDescription: '',
    sourceType: 'Risk',
    sourceReference: '',
    actionOwnerUserId: '',
    actionDueDate: '',
    actionStatus: 'Açık',
    actionProgress: 0,
    actionPriority: 'Orta'
};

function ProjectActionsPage() {
    const { id: projectId } = useParams();
    const { addAlert } = useAlert();

    const [actions, setActions] = useState([]);
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [form, setForm] = useState(emptyForm);
    const [editingActionId, setEditingActionId] = useState(null);
    const [showActionModal, setShowActionModal] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isDeletingId, setIsDeletingId] = useState(null);
    const actionPagination = usePagination(actions);
    const canWrite = canWriteProject();

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

    const fetchActions = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await projectService.getProjectActions(projectId);
            setActions(Array.isArray(data) ? data : []);
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Aksiyon kayıtları yüklenirken bir hata oluştu.';
            setError(backendMessage);
        } finally {
            setLoading(false);
        }
    };

    const fetchUsers = async () => {
        try {
            const userData = await projectService.getUsers();
            setUsers(getAssignableProjectUsers(userData));
        } catch (err) {
            console.error('Kullanıcı listesi alınamadı:', err);
            setUsers([]);
        }
    };

    const loadProjectData = useEffectEvent(() => {
        void fetchActions();
        void fetchUsers();
    });

    useEffect(() => {
        if (!projectId) return undefined;

        const timeoutId = window.setTimeout(loadProjectData, 0);
        return () => window.clearTimeout(timeoutId);
    }, [projectId]);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setForm((prev) => ({ ...prev, [name]: value }));
    };

    const resetForm = () => {
        setEditingActionId(null);
        setForm({
            ...emptyForm,
            actionOwnerUserId: getDefaultProjectAssigneeId(users)
        });
    };

    const openCreateModal = () => {
        resetForm();
        setShowActionModal(true);
    };

    const closeActionModal = () => {
        setShowActionModal(false);
        resetForm();
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!projectId) return;

        setIsSubmitting(true);
        setError(null);

        try {
            if (!form.actionDueDate) {
                setError('Bitiş tarihi zorunludur.');
                setIsSubmitting(false);
                return;
            }

            if (!form.sourceType || !SOURCE_TYPES.includes(form.sourceType)) {
                setError('Geçerli bir Kaynak Türü seçiniz.');
                setIsSubmitting(false);
                return;
            }

            const payload = {
                projectId,
                actionDescription: form.actionDescription,
                sourceType: form.sourceType,
                sourceReference: form.sourceReference || '',
                actionOwnerUserId: form.actionOwnerUserId,
                actionDueDate: new Date(form.actionDueDate).toISOString(),
                actionStatus: form.actionStatus || 'Açık',
                actionProgress: Number(form.actionProgress || 0),
                actionPriority: form.actionPriority || 'Orta'
            };

            if (editingActionId) {
                await projectService.updateAction(editingActionId, payload);
            } else {
                await projectService.createProjectAction(projectId, payload);
            }

            closeActionModal();
            await fetchActions();
            addAlert(editingActionId ? 'Aksiyon güncellendi.' : 'Yeni aksiyon eklendi.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Aksiyon kaydı işlenemedi.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleEdit = (action) => {
        setEditingActionId(action.actionId);
        setForm({
            actionDescription: action.actionDescription || '',
            sourceType: action.sourceType || '',
            sourceReference: action.sourceReference || '',
            actionOwnerUserId: getValidProjectAssigneeId(users, action.actionOwnerUserId),
            actionDueDate: action.actionDueDate ? new Date(action.actionDueDate).toISOString().slice(0, 10) : '',
            actionStatus: action.actionStatus || 'Açık',
            actionProgress: action.actionProgress ?? 0,
            actionPriority: action.actionPriority || 'Orta'
        });
        setShowActionModal(true);
    };

    const handleSoftDelete = async (action) => {
        if (!window.confirm('Bu aksiyonu silmek yerine durumunu "İptal" yaparak soft delete yapmak istediğinize emin misiniz?')) {
            return;
        }

        setIsDeletingId(action.actionId);
        setError(null);

        try {
            await projectService.updateAction(action.actionId, {
 
                actionStatus: 'İptal',
                sourceType: action.sourceType || 'Diğer',
                actionPriority: action.actionPriority || 'Orta',
                actionDescription: action.actionDescription,
                actionOwnerUserId: action.actionOwnerUserId
            });
            await fetchActions();
            addAlert('Aksiyon başarıyla iptal edildi.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Aksiyon kapatılamadı.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsDeletingId(null);
        }
    };

    if (loading) return <div className="tab-content-wrapper fade-in"><p className="status-text">Aksiyon kayıtları yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p className="status-text error-text">Hata: {error}</p></div>;

    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card action-card">
                <div className="card-header action-header">
                    <h2>Proje Aksiyonları (Actions)</h2>
                    {canWrite && (
                        <button type="button" className="btn-primary" onClick={openCreateModal}>
                            Yeni Aksiyon
                        </button>
                    )}
                </div>

                {!canWrite && (
                    <div className="permission-note">Bu projede aksiyon ekleme/düzenleme yetkiniz bulunmuyor.</div>
                )}

                {showActionModal && canWrite && (
                    <div className="action-modal-overlay" onClick={closeActionModal}>
                        <div className="action-modal" onClick={(e) => e.stopPropagation()}>
                            <div className="action-modal-header">
                                <h3>{editingActionId ? 'Aksiyon Düzenle' : 'Yeni Aksiyon Ekle'}</h3>
                                <button type="button" className="modal-close-btn" onClick={closeActionModal}>×</button>
                            </div>

                            <form className="action-form" onSubmit={handleSubmit}>
                                <div className="form-grid">
                                    <label className="full-width">
                                        <span>Aksiyon Tanımı</span>
                                        <textarea name="actionDescription" value={form.actionDescription} onChange={handleInputChange} rows="3" required />
                                    </label>
                                    <label>
                                        <span>Kaynak Türü</span>
                                        <select name="sourceType" value={form.sourceType} onChange={handleInputChange} required>
                                            {SOURCE_TYPES.map(type => (
                                                <option key={type} value={type}>{type}</option>
                                            ))}
                                        </select>
                                    </label>
                                    <label>
                                        <span>Kaynak Referans</span>
                                        <input name="sourceReference" value={form.sourceReference} onChange={handleInputChange} placeholder="Ref / Referans" />
                                    </label>
                                    <label>
                                        <span>Öncelik</span>
                                        <select name="actionPriority" value={form.actionPriority} onChange={handleInputChange}>
                                            <option value="Düşük">Düşük</option>
                                            <option value="Orta">Orta</option>
                                            <option value="Yüksek">Yüksek</option>
                                            <option value="Kritik">Kritik</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Durum</span>
                                        <select name="actionStatus" value={form.actionStatus} onChange={handleInputChange}>
                                            <option value="Açık">Açık</option>
                                            <option value="Devam Ediyor">Devam Ediyor</option>
                                            <option value="Tamamlandı">Tamamlandı</option>
                                            <option value="İptal">İptal</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>İlerleme %</span>
                                        <input type="number" min="0" max="100" name="actionProgress" value={form.actionProgress} onChange={handleInputChange} />
                                    </label>
                                    <label>
                                        <span>Hedef Tarihi</span>
                                        <input type="date" name="actionDueDate" value={form.actionDueDate} onChange={handleInputChange} />
                                    </label>
                                    <label>
                                        <span>Sorumlu Kullanıcı</span>
                                        <select name="actionOwnerUserId" value={form.actionOwnerUserId} onChange={handleInputChange} required>
                                            <option value="">-- Proje Yöneticisi Seçiniz --</option>
                                            {users.map((user) => {
                                                const userId = getUserRecordId(user);
                                                const userName = user.fullName || user.FullName || user.userName || user.UserName || user.name || userId;
                                                return (
                                                    <option key={userId} value={userId}>
                                                        {userName}
                                                    </option>
                                                );
                                            })}
                                        </select>
                                    </label>
                                </div>

                                <div className="form-actions">
                                    <button type="submit" className="btn-primary" disabled={isSubmitting}>
                                        {isSubmitting ? 'Kaydediliyor...' : editingActionId ? 'Güncelle' : 'Ekle'}
                                    </button>
                                    <button type="button" className="btn-secondary" onClick={closeActionModal}>
                                        İptal
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}

                <div className="table-responsive">
                    {actions.length === 0 ? (
                        <p className="empty-state">Bu projeye ait henüz bir aksiyon kaydı bulunmuyor.</p>
                    ) : (
                        <table className="modern-table action-table">
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Aksiyon Tanımı</th>
                                    <th>Kaynak / Ref</th>
                                    <th>Öncelik</th>
                                    <th>Durum / İlerleme</th>
                                    <th>Sorumlu</th>
                                    <th>Tarih Detayları</th>
                                    {canWrite && <th>İşlem</th>}
                                </tr>
                            </thead>
                            <tbody>
                                {actionPagination.paginatedItems.map((a) => {
                                    const priorityBadge = getPriorityStyle(a.actionPriority);

                                    return (
                                        <tr key={a.actionId}>
                                            <td><strong>{a.actionId}</strong></td>
                                            <td><div className="font-medium">{a.actionDescription || '-'}</div></td>
                                            <td>
                                                <div>{a.sourceType || '-'}</div>
                                                {a.sourceReference && (
                                                    <small className="secondary-text">Ref: {a.sourceReference}</small>
                                                )}
                                            </td>
                                            <td>
                                                <span className="priority-badge" style={{ backgroundColor: priorityBadge.bg, color: priorityBadge.text }}>
                                                    {a.actionPriority || 'Belirtilmemiş'}
                                                </span>
                                            </td>
                                            <td>
                                                <div>{a.actionStatus || '-'}</div>
                                                {a.actionProgress !== undefined && a.actionProgress !== null && (
                                                    <div className="progress-text">İlerleme: %{a.actionProgress}</div>
                                                )}
                                            </td>
                                            <td>
                                                <div>{a.actionOwnerFullName || a.actionOwnerUser?.fullName || 'Atanmamış'}</div>
                                                {a.actionOwnerUserId && (
                                                    <small className="secondary-text">User ID: {a.actionOwnerUserId}</small>
                                                )}
                                            </td>
                                            <td>
                                                <div className="date-detail-list">
                                                    <div><strong>Hedef:</strong> {formatDate(a.actionDueDate)}</div>
                                                    <div><strong>Tamamlanma:</strong> {formatDate(a.completedDate)}</div>
                                                </div>
                                            </td>
                                            {canWrite && (
                                                <td>
                                                    <div className="row-actions">
                                                        <button type="button" className="btn-secondary" onClick={() => handleEdit(a)}>
                                                            Düzenle
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="btn-danger"
                                                            onClick={() => handleSoftDelete(a)}
                                                            disabled={isDeletingId === a.actionId}
                                                        >
                                                            {isDeletingId === a.actionId ? 'Kapatılıyor...' : 'Sil'}
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
                        currentPage={actionPagination.currentPage}
                        itemLabel="aksiyon"
                        onPageChange={actionPagination.setCurrentPage}
                        totalItems={actionPagination.totalItems}
                        totalPages={actionPagination.totalPages}
                    />
                </div>
            </div>
        </div>
    );
}

export default ProjectActionsPage;
