import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import Pagination from '../components/Pagination';
import { useAlert } from '../components/AlertProvider';
import { canWriteProject } from '../utils/permissionHelper';
import { usePagination } from '../utils/usePagination';
import './EvmRecordsPage.css';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5074';

const getMetricStatusStyle = (metricType, value) => {
    if (value === null || value === undefined || Number.isNaN(Number(value))) {
        return { bg: '#f3f4f6', text: '#374151' };
    }
    const num = Number(value);
    const GREEN = { bg: '#d1fae5', text: '#065f46' };
    const YELLOW = { bg: '#fef3c7', text: '#92400e' };
    const RED = { bg: '#fee2e2', text: '#991b1b' };

    switch (metricType) {
        case 'spi':
        case 'cpi':
            if (num >= 0.95) return GREEN;
            if (num >= 0.85) return YELLOW;
            return RED;
        default:
            return { bg: '#f3f4f6', text: '#374151' };
    }
};

function EvmRecordsPage() {
    const { id: projectId } = useParams();
    const { addAlert } = useAlert();
    const [records, setRecords] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [successMessage, setSuccessMessage] = useState('');
    const [showForm, setShowForm] = useState(false);
    const [editingRecordId, setEditingRecordId] = useState(null);
    const [formValues, setFormValues] = useState({ period: '', bac: '', pv: '', ev: '', ac: '' });
    const canManage = canWriteProject();
    const recordPagination = usePagination(records);

    const formatCurrency = (val, currencyCode = 'TRY') => {
        if (val === null || val === undefined) return '-';
        try {
            return new Intl.NumberFormat('tr-TR', {
                style: 'currency',
                currency: currencyCode
            }).format(val);
        } catch {
            return `${val} ${currencyCode}`;
        }
    };

    const fetchEvmRecords = useCallback(async () => {
        try {
            setError(null);
            const token = localStorage.getItem('token');
            const response = await fetch(`${API_BASE_URL}/projects/${projectId}/evm-records`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            if (!response.ok) throw new Error('EVM verileri alınamadı.');

            const data = await response.json();
            setRecords(data);
        } catch {
            setError('EVM verileri alınamadı.');
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        if (!projectId) return;

        fetchEvmRecords();
    }, [projectId, fetchEvmRecords]);

    const resetForm = () => {
        setShowForm(false);
        setEditingRecordId(null);
        setFormValues({ period: '', bac: '', pv: '', ev: '', ac: '' });
    };

    const handleInputChange = (event) => {
        const { name, value } = event.target;
        setFormValues((prev) => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (event) => {
        event.preventDefault();

        try {
            const token = localStorage.getItem('token');
            const payload = {
                projectId,
                period: formValues.period,
                bac: Number(formValues.bac),
                pv: Number(formValues.pv),
                ev: Number(formValues.ev),
                ac: Number(formValues.ac)
            };

            const url = editingRecordId
                ? `${API_BASE_URL}/evm-records/${editingRecordId}`
                : `${API_BASE_URL}/evm-records`;
            const method = editingRecordId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method,
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json().catch(() => null);
            if (!response.ok) throw new Error(data?.message || 'İşlem başarısız oldu.');

            setSuccessMessage(data?.message || 'EVM kaydı kaydedildi.');
            addAlert(data?.message || 'EVM kaydı kaydedildi.', 'success');
            resetForm();
            await fetchEvmRecords();
        } catch (err) {
            setError(err.message);
            addAlert(err.message, 'error');
        }
    };

    const handleDelete = async (recordId) => {
        if (!window.confirm('Bu EVM kaydını silmek istediğinize emin misiniz?')) {
            return;
        }

        try {
            const token = localStorage.getItem('token');
            const response = await fetch(`${API_BASE_URL}/evm-records/${recordId}`, {
                method: 'DELETE',
                headers: { Authorization: `Bearer ${token}` }
            });
            const data = await response.json().catch(() => null);
            if (!response.ok) throw new Error(data?.message || 'Silme işlemi başarısız oldu.');

            setSuccessMessage(data?.message || 'EVM kaydı silindi.');
            addAlert(data?.message || 'EVM kaydı silindi.', 'success');
            await fetchEvmRecords();
        } catch (err) {
            setError(err.message);
            addAlert(err.message, 'error');
        }
    };

    const openCreateForm = () => {
        setEditingRecordId(null);
        setFormValues({ period: '', bac: '', pv: '', ev: '', ac: '' });
        setShowForm(true);
    };

    const openEditForm = (record) => {
        setEditingRecordId(record.evmRecordId);
        setFormValues({
            period: record.period || '',
            bac: record.bac ?? '',
            pv: record.pv ?? '',
            ev: record.ev ?? '',
            ac: record.ac ?? ''
        });
        setShowForm(true);
    };

    if (loading) return <div style={{ padding: '20px', fontSize: '18px' }}>Yükleniyor...</div>;
    if (error && records.length === 0) {
        return <div style={{ padding: '20px', color: 'red', fontSize: '18px' }}>Hata: {error}</div>;
    }

    const chartData = [...records].reverse();

    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card" style={{ padding: '24px' }}>
                <div className="evm-page-header">
                    <h2>Kazanılmış Değer Yönetimi (EVM)</h2>
                    {canManage && (
                        <button className="evm-action-button" type="button" onClick={openCreateForm}>
                            + Yeni EVM Kaydı
                        </button>
                    )}
                </div>

                {successMessage && <div className="evm-alert success">{successMessage}</div>}
                {error && <div className="evm-alert error">{error}</div>}

                {canManage && showForm && (
                    <form className="evm-form-card" onSubmit={handleSubmit}>
                        <h3>{editingRecordId ? 'EVM Kaydını Güncelle' : 'EVM Kaydı Ekle'}</h3>
                        <div className="evm-form-grid">
                            <label>
                                Dönem
                                <input name="period" value={formValues.period} onChange={handleInputChange} required placeholder="YYYY-AA" />
                            </label>
                            <label>
                                BAC
                                <input name="bac" type="number" step="0.01" value={formValues.bac} onChange={handleInputChange} required />
                            </label>
                            <label>
                                PV
                                <input name="pv" type="number" step="0.01" value={formValues.pv} onChange={handleInputChange} required />
                            </label>
                            <label>
                                EV
                                <input name="ev" type="number" step="0.01" value={formValues.ev} onChange={handleInputChange} required />
                            </label>
                            <label>
                                AC
                                <input name="ac" type="number" step="0.01" value={formValues.ac} onChange={handleInputChange} required />
                            </label>
                        </div>
                        <div className="evm-form-actions">
                            <button className="evm-action-button" type="submit">{editingRecordId ? 'Güncelle' : 'Kaydet'}</button>
                            <button className="evm-secondary-button" type="button" onClick={resetForm}>İptal</button>
                        </div>
                    </form>
                )}

                <div className="table-responsive" style={{ marginBottom: '40px' }}>
                    <table className="modern-table" style={{ width: '100%', fontSize: '17px' }}>
                        <thead>
                            <tr>
                                <th>Dönem</th>
                                <th>BAC</th>
                                <th>PV</th>
                                <th>EV</th>
                                <th>AC</th>
                                <th>SPI (Zaman)</th>
                                <th>CPI (Maliyet)</th>
                                <th>EAC</th>
                                <th>VAC</th>
                                {canManage && <th>İşlem</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {recordPagination.paginatedItems.map((r) => {
                                const spiStyle = getMetricStatusStyle('spi', r.spi);
                                const cpiStyle = getMetricStatusStyle('cpi', r.cpi);
                                const currentCurrency = r.currency || 'TRY';
                                const vacValue = r.vac ?? 0;

                                return (
                                    <tr key={r.evmRecordId}>
                                        <td><strong>{r.period}</strong></td>
                                        <td>{formatCurrency(r.bac, currentCurrency)}</td>
                                        <td style={{ color: '#2563eb' }}>{formatCurrency(r.pv, currentCurrency)}</td>
                                        <td style={{ color: '#16a34a' }}>{formatCurrency(r.ev, currentCurrency)}</td>
                                        <td style={{ color: '#dc2626' }}>{formatCurrency(r.ac, currentCurrency)}</td>

                                        <td>
                                            <span style={{ backgroundColor: spiStyle.bg, color: spiStyle.text, padding: '6px 12px', borderRadius: '8px', fontWeight: 'bold', fontSize: '15px' }}>
                                                {r.spi ?? '-'}
                                            </span>
                                        </td>
                                        <td>
                                            <span style={{ backgroundColor: cpiStyle.bg, color: cpiStyle.text, padding: '6px 12px', borderRadius: '8px', fontWeight: 'bold', fontSize: '15px' }}>
                                                {r.cpi ?? '-'}
                                            </span>
                                        </td>

                                        <td>{formatCurrency(r.eac, currentCurrency)}</td>
                                        <td style={{ color: vacValue < 0 ? '#dc2626' : '#16a34a', fontWeight: 'bold' }}>
                                            {formatCurrency(vacValue, currentCurrency)}
                                        </td>
                                        {canManage && (
                                            <td>
                                                <div className="evm-row-actions">
                                                    <button className="evm-link-button" type="button" onClick={() => openEditForm(r)}>Düzenle</button>
                                                    <button className="evm-link-button danger" type="button" onClick={() => handleDelete(r.evmRecordId)}>Sil</button>
                                                </div>
                                            </td>
                                        )}
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                    <Pagination
                        currentPage={recordPagination.currentPage}
                        itemLabel="EVM kaydı"
                        onPageChange={recordPagination.setCurrentPage}
                        totalItems={recordPagination.totalItems}
                        totalPages={recordPagination.totalPages}
                    />
                </div>

                <div style={{ backgroundColor: '#fff', padding: '16px', borderRadius: '12px', border: '1px solid #f1f5f9' }}>
                    <h3 style={{ fontSize: '18px', marginBottom: '20px' }}>PV, EV ve AC Trend Analizi</h3>
                    <ResponsiveContainer width="100%" height={350}>
                        <LineChart data={chartData}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey="period" />
                            <YAxis />
                            <Tooltip formatter={(val, name, props) => formatCurrency(val, props.payload.currency || 'TRY')} />
                            <Legend />
                            <Line type="monotone" dataKey="pv" name="PV (Planlanan)" stroke="#2563eb" strokeWidth={3} />
                            <Line type="monotone" dataKey="ev" name="EV (Kazanılan)" stroke="#16a34a" strokeWidth={3} />
                            <Line type="monotone" dataKey="ac" name="AC (Fiili)" stroke="#dc2626" strokeWidth={3} />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            </div>
        </div>
    );
}

export default EvmRecordsPage;
