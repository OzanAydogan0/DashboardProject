import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { projectService } from '../services/projectService';
import { useAlert } from '../components/AlertProvider';
import './ProjectRisksPage.css';

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
    riskTitle: '',
    riskCategory: 'Genel',
    riskProbability: 3,
    riskImpact: 3,
    riskStatus: 'Açık',
    riskDueDate: '',
    riskMitigation: '',
    riskOwnerUserId: getCurrentUserId()
};

const ProjectRisksPage = () => {
    const { id: projectId } = useParams();
    const { addAlert } = useAlert();

    const [risks, setRisks] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [form, setForm] = useState(emptyForm);
    const [editingRiskId, setEditingRiskId] = useState(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isDeletingId, setIsDeletingId] = useState(null);
    const [canWrite, setCanWrite] = useState(false);
    const [showRiskModal, setShowRiskModal] = useState(false);

    const fetchRisks = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await projectService.getProjectRisks(projectId);
            setRisks(Array.isArray(data) ? data : []);
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Risk verileri alınamadı.';
            setError(backendMessage);
        } finally {
            setLoading(false);
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
            fetchRisks();
        }
    }, [projectId]);

    const getScoreColor = (score) => {
        if (score >= 15) return '#ef4444';
        if (score >= 8) return '#f59e0b';
        return '#10b981';
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setForm((prev) => ({ ...prev, [name]: value }));
    };

    const resetForm = () => {
        setEditingRiskId(null);
        setForm({
            ...emptyForm,
            riskOwnerUserId: getCurrentUserId()
        });
    };

    const openCreateModal = () => {
        resetForm();
        setShowRiskModal(true);
    };

    const closeRiskModal = () => {
        setShowRiskModal(false);
        resetForm();
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!projectId) return;

        setIsSubmitting(true);
        setError(null);

        try {
            const payload = {
                ...form,
                projectId,
                riskProbability: Number(form.riskProbability),
                riskImpact: Number(form.riskImpact),
                riskDueDate: form.riskDueDate ? new Date(form.riskDueDate).toISOString() : null,
            };

            if (editingRiskId) {
                await projectService.updateRisk(editingRiskId, payload);
            } else {
                await projectService.createProjectRisk(projectId, payload);
            }

            resetForm();
            setShowRiskModal(false);
            await fetchRisks();
            addAlert(editingRiskId ? 'Risk kaydı güncellendi.' : 'Yeni risk kaydı eklendi.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Risk kaydı işlenemedi.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleEdit = (risk) => {
        setEditingRiskId(risk.riskId);
        setForm({
            riskTitle: risk.riskTitle || '',
            riskCategory: risk.riskCategory || 'Genel',
            riskProbability: risk.riskProbability ?? 3,
            riskImpact: risk.riskImpact ?? 3,
            riskStatus: risk.riskStatus || 'Açık',
            riskDueDate: risk.riskDueDate ? new Date(risk.riskDueDate).toISOString().slice(0, 10) : '',
            riskMitigation: risk.riskMitigation || '',
            riskOwnerUserId: risk.riskOwnerUserId || getCurrentUserId()
        });
        setShowRiskModal(true);
    };

    const handleDelete = async (riskId) => {
        if (!window.confirm('Bu risk kaydını silmek istediğinize emin misiniz?')) {
            return;
        }

        setIsDeletingId(riskId);
        try {
            await projectService.deleteRisk(riskId);
            await fetchRisks();
            addAlert('Risk kaydı başarıyla silindi.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Risk silinemedi.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsDeletingId(null);
        }
    };

    if (loading) return <div className="tab-content-wrapper fade-in"><p className="status-text">Risk verileri yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p className="status-text error-text">Hata: {error}</p></div>;

    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card project-risks-card">
                <div className="card-header project-risks-header">
                    <h2>Proje Risk Kayıtları</h2>
                    {canWrite && (
                        <button type="button" className="btn-primary" onClick={openCreateModal}>
                            Yeni Risk
                        </button>
                    )}
                </div>

                {!canWrite && (
                    <div className="permission-note">Bu projede risk ekleme/düzenleme yetkiniz bulunmuyor.</div>
                )}

                <div className="table-description">
                    Bu tabloda proje risklerinin başlığı, kategorisi, olasılığı, etkisi, skoru, azaltım/müdahale bilgisi ve sorumlusu görüntülenir.
                </div>

                {showRiskModal && canWrite && (
                    <div className="risk-modal-overlay" onClick={closeRiskModal}>
                        <div className="risk-modal" onClick={(e) => e.stopPropagation()}>
                            <div className="risk-modal-header">
                                <h3>{editingRiskId ? 'Risk Düzenle' : 'Yeni Risk Ekle'}</h3>
                                <button type="button" className="modal-close-btn" onClick={closeRiskModal}>×</button>
                            </div>

                            <form className="risk-form" onSubmit={handleSubmit}>
                                <div className="form-grid">
                                    <label>
                                        <span>Risk Başlığı</span>
                                        <input name="riskTitle" value={form.riskTitle} onChange={handleInputChange} required />
                                    </label>
                                    <label>
                                        <span>Kategori</span>
                                        <input name="riskCategory" value={form.riskCategory} onChange={handleInputChange} required />
                                    </label>
                                    <label>
                                        <span>Olasılık</span>
                                        <select name="riskProbability" value={form.riskProbability} onChange={handleInputChange}>
                                            <option value="1">1</option>
                                            <option value="2">2</option>
                                            <option value="3">3</option>
                                            <option value="4">4</option>
                                            <option value="5">5</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Etki</span>
                                        <select name="riskImpact" value={form.riskImpact} onChange={handleInputChange}>
                                            <option value="1">1</option>
                                            <option value="2">2</option>
                                            <option value="3">3</option>
                                            <option value="4">4</option>
                                            <option value="5">5</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Durum</span>
                                        <select name="riskStatus" value={form.riskStatus} onChange={handleInputChange}>
                                            <option value="Açık">Açık</option>
                                            <option value="İzleniyor">İzleniyor</option>
                                            <option value="Kapandı">Kapandı</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Bitiş Tarihi</span>
                                        <input type="date" name="riskDueDate" value={form.riskDueDate} onChange={handleInputChange} />
                                    </label>
                                    <label className="full-width">
                                        <span>Sorumlu Kullanıcı ID</span>
                                        <input name="riskOwnerUserId" value={form.riskOwnerUserId} onChange={handleInputChange} />
                                    </label>
                                    <label className="full-width">
                                        <span>Azaltım / Müdahale</span>
                                        <textarea name="riskMitigation" value={form.riskMitigation} onChange={handleInputChange} rows="3" />
                                    </label>
                                </div>

                                <div className="form-actions">
                                    <button type="submit" className="btn-primary" disabled={isSubmitting}>
                                        {isSubmitting ? 'Kaydediliyor...' : editingRiskId ? 'Güncelle' : 'Ekle'}
                                    </button>
                                    <button type="button" className="btn-secondary" onClick={closeRiskModal}>
                                        İptal
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}

                <div className="table-responsive">
                    {risks.length === 0 ? (
                        <p className="empty-state">Bu projeye ait henüz bir risk tanımlanmamış.</p>
                    ) : (
                        <table className="modern-table">
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Risk Başlığı</th>
                                    <th>Kategori</th>
                                    <th>Olasılık</th>
                                    <th>Etki</th>
                                    <th>Skor</th>
                                    <th>Durum</th>
                                    <th>Azaltım / Müdahale</th>
                                    <th>Sorumlu</th>
                                    <th>Bitiş Tarihi</th>
                                    {canWrite && <th>İşlem</th>}
                                </tr>
                            </thead>
                            <tbody>
                                {risks.map((risk) => (
                                    <tr key={risk.riskId}>
                                        <td>{risk.riskId}</td>
                                        <td className="font-medium">{risk.riskTitle}</td>
                                        <td>{risk.riskCategory || '-'}</td>
                                        <td><strong>{risk.riskProbability ?? '-'}</strong></td>
                                        <td><strong>{risk.riskImpact ?? '-'}</strong></td>
                                        <td>
                                            <span className="risk-score-pill" style={{ backgroundColor: getScoreColor(risk.riskScore) }}>
                                                {risk.riskScore ?? 0}
                                            </span>
                                        </td>
                                        <td>{risk.riskStatus || '-'}</td>
                                        <td className="mitigation-cell">{risk.riskMitigation || '-'}</td>
                                        <td>{risk.riskOwnerFullName || 'Atanmamış'}</td>
                                        <td>
                                            {risk.riskDueDate
                                                ? new Date(risk.riskDueDate).toLocaleDateString('tr-TR')
                                                : 'Belirtilmemiş'}
                                        </td>
                                        {canWrite && (
                                            <td>
                                                <div className="row-actions">
                                                    <button type="button" className="btn-secondary" onClick={() => handleEdit(risk)}>
                                                        Düzenle
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className="btn-danger"
                                                        onClick={() => handleDelete(risk.riskId)}
                                                        disabled={isDeletingId === risk.riskId}
                                                    >
                                                        {isDeletingId === risk.riskId ? 'Siliniyor...' : 'Sil'}
                                                    </button>
                                                </div>
                                            </td>
                                        )}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ProjectRisksPage;