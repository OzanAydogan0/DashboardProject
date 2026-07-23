import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';

/**
 * Proje Riskleri Sayfa Bileşeni (ProjectRisksPage)
 * - Proje Kodu ve Proje ID alanları kaldırılmıştır.
 * - Olasılık ve Etki değerleri ayrı sütunlarda gösterilmektedir.
 */
const ProjectRisksPage = () => {
    // 1. URL'den proje ID'sini alıyoruz
    const { id: projectId } = useParams();

    // 2. Sayfanın durumlarını (state) tanımlıyoruz
    const [risks, setRisks] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // 3. API'den risk verilerini çeken useEffect
    useEffect(() => {
        const fetchRisks = async () => {
            try {
                const token = localStorage.getItem('token'); 

                const response = await fetch(`http://localhost:5074/projects/${projectId}/risks`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    }
                });

                if (response.status === 401) throw new Error("Oturum süreniz dolmuş, lütfen tekrar giriş yapın.");
                if (response.status === 403) throw new Error("Bu projenin risk verilerini görmeye yetkiniz yok!");
                if (!response.ok) throw new Error("Riskler yüklenirken bir hata oluştu.");

                const data = await response.json();
                setRisks(data);

            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        if (projectId) {
            fetchRisks();
        }
    }, [projectId]);

    // 4. Risk skoruna göre rozet (badge) rengini belirleyen fonksiyon
    const getScoreColor = (score) => {
        if (score >= 15) return '#ef4444'; // Yüksek Risk: Kırmızı
        if (score >= 8) return '#f59e0b';  // Orta Risk: Turuncu
        return '#10b981';                  // Düşük Risk: Yeşil
    };

    // 5. Yüklenme ve Hata Durumları
    if (loading) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px' }}>Risk verileri yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px', color: 'red' }}>Hata: {error}</p></div>;

    // 6. Ana Tablo Ekranı
    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card">
                
                <div className="card-header">
                    <h2>Proje Risk Kayıtları</h2>
                </div>

                <div className="table-responsive">
                    {risks.length === 0 ? (
                        <p style={{ padding: '20px', textAlign: 'center' }}>Bu projeye ait henüz bir risk tanımlanmamış.</p>
                    ) : (
                        <table className="modern-table" style={{ width: '100%', textAlign: 'left' }}>
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Risk Başlığı</th>
                                    <th>Kategori</th>
                                    <th>Olasılık</th>
                                    <th>Etki</th>
                                    <th>Skor</th>
                                    <th>Durum</th>
                                    <th>Sorumlu</th>
                                    <th>Bitiş Tarihi</th>
                                </tr>
                            </thead>
                            <tbody>
                                {risks.map((risk) => (
                                    <tr key={risk.riskId}>
                                        {/* 1. Risk ID */}
                                        <td>{risk.riskId}</td>

                                        {/* 2. Risk Başlığı */}
                                        <td className="font-medium">{risk.riskTitle}</td>

                                        {/* 3. Kategori */}
                                        <td>{risk.riskCategory || '-'}</td>

                                        {/* 4. Olasılık (Ayrı Sütun) */}
                                        <td>
                                            <strong>{risk.riskProbability ?? '-'}</strong>
                                        </td>

                                        {/* 5. Etki (Ayrı Sütun) */}
                                        <td>
                                            <strong>{risk.riskImpact ?? '-'}</strong>
                                        </td>

                                        {/* 6. Risk Skoru */}
                                        <td>
                                            <span style={{ 
                                                backgroundColor: getScoreColor(risk.riskScore), 
                                                color: 'white', 
                                                padding: '4px 8px', 
                                                borderRadius: '12px',
                                                fontWeight: 'bold',
                                                display: 'inline-block'
                                            }}>
                                                {risk.riskScore ?? 0}
                                            </span>
                                        </td>

                                        {/* 7. Durum */}
                                        <td>{risk.riskStatus || '-'}</td>

                                        {/* 8. Sorumlu */}
                                        <td>{risk.riskOwnerFullName || 'Atanmamış'}</td>

                                        {/* 9. Bitiş Tarihi */}
                                        <td>
                                            {risk.riskDueDate 
                                                ? new Date(risk.riskDueDate).toLocaleDateString('tr-TR') 
                                                : 'Belirtilmemiş'}
                                        </td>
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