import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

// Renk Fonksiyonu
const getMetricStatusStyle = (metricType, value) => {
    if (value === null || value === undefined || isNaN(value)) {
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
    const [records, setRecords] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const formatCurrency = (val) => {
        if (val === null || val === undefined) return '-';
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(val);
    };

    useEffect(() => {
        const fetchEvmRecords = async () => {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch(`http://localhost:5074/projects/${projectId}/evm-records`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (!response.ok) throw new Error("EVM verileri alınamadı.");
                const data = await response.json();
                setRecords(data);
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        if (projectId) fetchEvmRecords();
    }, [projectId]);

    if (loading) return <div style={{ padding: '20px', fontSize: '18px' }}>Yükleniyor...</div>;
    if (error) return <div style={{ padding: '20px', color: 'red', fontSize: '18px' }}>Hata: {error}</div>;

    const chartData = [...records].reverse();

    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card" style={{ padding: '24px' }}>
                <h2>Kazanılmış Değer Yönetimi (EVM)</h2>

                {/* 1. YENİ RENK KURALLARINA GÖRE EVM TABLOSU */}
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
                            </tr>
                        </thead>
                        <tbody>
                            {records.map((r) => {
                                // Yeni renk kurallarının uygulanması
                                const spiStyle = getMetricStatusStyle('spi', r.spi);
                                const cpiStyle = getMetricStatusStyle('cpi', r.cpi);

                                return (
                                    <tr key={r.evmRecordId}>
                                        <td><strong>{r.period}</strong></td>
                                        <td>{formatCurrency(r.bac)}</td>
                                        <td style={{ color: '#2563eb' }}>{formatCurrency(r.pv)}</td>
                                        <td style={{ color: '#16a34a' }}>{formatCurrency(r.ev)}</td>
                                        <td style={{ color: '#dc2626' }}>{formatCurrency(r.ac)}</td>
                                        
                                        {/* SPI Rozeti (Yeni Kurallarla) */}
                                        <td>
                                            <span style={{
                                                backgroundColor: spiStyle.bg,
                                                color: spiStyle.text,
                                                padding: '6px 12px',
                                                borderRadius: '8px',
                                                fontWeight: 'bold',
                                                fontSize: '15px'
                                            }}>
                                                {r.spi ?? '-'}
                                            </span>
                                        </td>

                                        {/* CPI Rozeti (Yeni Kurallarla) */}
                                        <td>
                                            <span style={{
                                                backgroundColor: cpiStyle.bg,
                                                color: cpiStyle.text,
                                                padding: '6px 12px',
                                                borderRadius: '8px',
                                                fontWeight: 'bold',
                                                fontSize: '15px'
                                            }}>
                                                {r.cpi ?? '-'}
                                            </span>
                                        </td>

                                        <td>{formatCurrency(r.eac)}</td>
                                        <td style={{ color: r.vac < 0 ? '#dc2626' : '#16a34a', fontWeight: 'bold' }}>
                                            {formatCurrency(r.vac)}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>

                {/* 2. TREND ÇİZGİ GRAFİĞİ */}
                <div style={{ backgroundColor: '#fff', padding: '16px', borderRadius: '12px', border: '1px solid #f1f5f9' }}>
                    <h3 style={{ fontSize: '18px', marginBottom: '20px' }}>PV, EV ve AC Trend Analizi</h3>
                    <ResponsiveContainer width="100%" height={350}>
                        <LineChart data={chartData}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey="period" />
                            <YAxis />
                            <Tooltip formatter={(val) => formatCurrency(val)} />
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