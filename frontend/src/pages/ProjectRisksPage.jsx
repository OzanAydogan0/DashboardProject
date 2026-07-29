import { useState, useEffect, useEffectEvent, useRef } from 'react';
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
import './ProjectRisksPage.css';

const emptyForm = {
    riskTitle: '',
    riskCategory: 'Genel',
    riskProbability: 3,
    riskImpact: 3,
    riskStatus: 'Açık',
    riskDueDate: '',
    riskMitigation: '',
    riskOwnerUserId: ''
};

const ProjectRisksPage = () => {
    const { id: projectId } = useParams();
    const { addAlert } = useAlert();
    const fileInputRef = useRef(null);

    const [risks, setRisks] = useState([]);
    const [users, setUsers] = useState([]); // 📌 EKLENDİ: Kullanıcı listesini tutacak state
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [form, setForm] = useState(emptyForm);
    const [editingRiskId, setEditingRiskId] = useState(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isDeletingId, setIsDeletingId] = useState(null);
    const [isImporting, setIsImporting] = useState(false);
    const [showRiskModal, setShowRiskModal] = useState(false);
    const canWrite = canWriteProject();
    const riskPagination = usePagination(risks);

    // Risk verilerini getiren fonksiyon
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

    // 📌 EKLENDİ: Veritabanından kullanıcı listesini çeken fonksiyon
    const fetchUsers = async () => {
        try {
            // Servisinizde kullanıcıları getiren metodu çağırıyoruz
            const userData = await projectService.getUsers(); 
            setUsers(getAssignableProjectUsers(userData));
        } catch (err) {
            console.error('Kullanıcı listesi alınamadı:', err);
        }
    };

    const loadProjectData = useEffectEvent(() => {
        void fetchRisks();
        void fetchUsers();
    });

    useEffect(() => {
        if (!projectId) return undefined;

        const timeoutId = window.setTimeout(loadProjectData, 0);
        return () => window.clearTimeout(timeoutId);
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
            riskOwnerUserId: getDefaultProjectAssigneeId(users)
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
                riskOwnerUserId: form.riskOwnerUserId,
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
            riskOwnerUserId: getValidProjectAssigneeId(users, risk.riskOwnerUserId)
        });
        setShowRiskModal(true);
    };

    const handleCloseRisk = async (riskId) => {
        if (!window.confirm('Bu risk kaydını kapatmak istediğinize emin misiniz?')) {
            return;
        }

        setIsDeletingId(riskId);
        try {
            await projectService.updateRisk(riskId, { riskStatus: 'Kapalı' });
            await fetchRisks();
            addAlert('Risk kaydı başarıyla kapatıldı.', 'success');
        } catch (err) {
            const backendMessage = err.response?.data?.message || err.message || 'Risk kapatılamadı.';
            setError(backendMessage);
            addAlert(backendMessage, 'error');
        } finally {
            setIsDeletingId(null);
        }
    };

    const handleExcelImport = async (event) => {
        const file = event.target.files?.[0];
        if (!file || !projectId) return;

        setIsImporting(true);

        try {
            const formData = new FormData();
            formData.append('file', file);

            const response = await projectService.importProjectRisksExcel(projectId, formData);
            const result = response.data || response;
            const isSuccess = result.success ?? result.Success;
            const importedCount = result.totalImported ?? result.TotalImported ?? 0;
            const failedCount = result.totalFailed ?? result.TotalFailed ?? 0;
            const errors = result.errors ?? result.Errors ?? [];

            if (isSuccess && failedCount === 0) {
                addAlert(`Excel içe aktarma tamamlandı. Eklenen risk sayısı: ${importedCount}`, 'success');
            } else {
                const errorDetails = errors.length > 0 ? ` Hata detayları: ${errors.join(' • ')}` : '';
                addAlert(
                    `İşlem tamamlandı. Başarılı: ${importedCount}, Hatalı/Atlanan: ${failedCount}.${errorDetails}`,
                    'info'
                );
            }

            await fetchRisks();
        } catch (err) {
            const message = err.response?.data?.message || 'Excel dosyası yüklenirken sunucuda bir hata oluştu.';
            addAlert(message, 'error');
        } finally {
            setIsImporting(false);
            event.target.value = '';
            if (fileInputRef.current) {
                fileInputRef.current.value = '';
            }
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
                        <div className="project-risks-header-actions">
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept=".xlsx"
                                onChange={handleExcelImport}
                                hidden
                            />
                            <button
                                type="button"
                                className="import-project-btn"
                                onClick={() => fileInputRef.current?.click()}
                                disabled={isImporting}
                                title="Skor sütunu eklemeyin; skor Olasılık × Etki ile otomatik hesaplanır"
                                style={{
                                    backgroundColor: '#10b981',
                                    color: 'white',
                                    border: 'none',
                                    padding: '8px 16px',
                                    borderRadius: '6px',
                                    cursor: 'pointer',
                                    fontWeight: 'bold'
                                }}
                            >
                                {isImporting ? '⏳ Aktarılıyor...' : '📂 Excel İçe Aktar'}
                            </button>
                            <button type="button" className="btn-primary" onClick={openCreateModal}>
                                Yeni Risk
                            </button>
                        </div>
                    )}
                </div>

                {!canWrite && (
                    <div className="permission-note">Bu projede risk ekleme/düzenleme yetkiniz bulunmuyor.</div>
                )}

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
                                            <option value="Kapalı">Kapalı</option>
                                        </select>
                                    </label>
                                    <label>
                                        <span>Bitiş Tarihi</span>
                                        <input type="date" name="riskDueDate" value={form.riskDueDate} onChange={handleInputChange} />
                                    </label>
                                    
                                    {/* 📌 GÜNCELLENEN ALAN: Sorumlu Kullanıcı Dropdown Menüsü */}
                                    <label className="full-width">
                                        <span>Sorumlu Kullanıcı</span>
                                        <select 
                                            name="riskOwnerUserId" 
                                            value={form.riskOwnerUserId} 
                                            onChange={handleInputChange}
                                            required
                                        >
                                            <option value="">-- Sorumlu Seçiniz --</option>
                                            {users.map((u) => {
                                                const userId = getUserRecordId(u);
                                                const userName = u.fullName || u.FullName || u.userName || u.name || userId;
                                                return (
                                                    <option key={userId} value={userId}>
                                                        {userName}
                                                    </option>
                                                );
                                            })}
                                        </select>
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
                                {riskPagination.paginatedItems.map((risk) => (
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
                                                        onClick={() => handleCloseRisk(risk.riskId)}
                                                        disabled={isDeletingId === risk.riskId || risk.riskStatus === 'Kapalı'}
                                                    >
                                                        {isDeletingId === risk.riskId ? 'Kapatılıyor...' : risk.riskStatus === 'Kapalı' ? 'Kapalı' : 'Kapat'}
                                                    </button>
                                                </div>
                                            </td>
                                        )}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                    <Pagination
                        currentPage={riskPagination.currentPage}
                        itemLabel="risk"
                        onPageChange={riskPagination.setCurrentPage}
                        totalItems={riskPagination.totalItems}
                        totalPages={riskPagination.totalPages}
                    />
                </div>
            </div>
        </div>
    );
};

export default ProjectRisksPage;
