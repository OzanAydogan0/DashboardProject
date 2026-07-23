import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';

/**
 * Sorun / Problem Kayıtları (Problems / Issues) Tablo Bileşeni
 * Güncelleme: Genel punto boyutları (fontSize) belirgin şekilde büyütüldü.
 */
function ProblemsPage() {
    // 1. URL'den proje ID'sini alıyoruz
    const { id: projectId } = useParams();

    // 2. State tanımlamaları
    const [issues, setIssues] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // 3. Tarih Biçimlendirme Yardımcı Fonksiyonu
    const formatDate = (dateString) => {
        if (!dateString) return '-';
        return new Date(dateString).toLocaleDateString('tr-TR');
    };

    // 4. Önceliğe göre renk belirleyen yardımcı fonksiyon
    const getPriorityStyle = (priority) => {
        switch (priority?.toLowerCase()) {
            case 'yüksek':
            case 'high':
                return { bg: '#fee2e2', text: '#991b1b' }; // Açık Kırmızı
            case 'orta':
            case 'medium':
                return { bg: '#fef3c7', text: '#92400e' }; // Açık Turuncu
            case 'düşük':
            case 'low':
                return { bg: '#d1fae5', text: '#065f46' }; // Açık Yeşil
            default:
                return { bg: '#f3f4f6', text: '#374151' }; // Gri
        }
    };

    // 5. API'den tüm verileri çeken useEffect
    useEffect(() => {
        const fetchIssues = async () => {
            try {
                const token = localStorage.getItem('token');

                const response = await fetch(`http://localhost:5074/projects/${projectId}/issues`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    }
                });

                if (response.status === 401) throw new Error("Oturum süreniz dolmuş, lütfen tekrar giriş yapın.");
                if (response.status === 403) throw new Error("Bu projenin sorun verilerini görmeye yetkiniz yok!");
                if (!response.ok) throw new Error("Sorun kayıtları yüklenirken bir hata oluştu.");

                const data = await response.json();
                setIssues(data);

            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        if (projectId) {
            fetchIssues();
        }
    }, [projectId]);

    // 6. Yüklenme ve Hata Durumları (Puntolar 18px yapıldı)
    if (loading) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px', fontSize: '18px' }}>Sorunlar ve çözüm detayları yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px', color: 'red', fontSize: '18px' }}>Hata: {error}</p></div>;

    // 7. Ana Ekran ve Büyütülmüş Puntolu Tablo
    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card">
                
                <div className="card-header">
                    <h2>Sorun (Issue) Kayıtları</h2>
                </div>

                <div className="table-responsive">
                    {issues.length === 0 ? (
                        <p style={{ padding: '20px', textAlign: 'center', fontSize: '18px' }}>Bu projeye ait henüz bir sorun kaydı bulunmuyor.</p>
                    ) : (
                        /* Tablo temel puntosu 17px seviyesine yükseltildi */
                        <table className="modern-table" style={{ width: '100%', textAlign: 'left', fontSize: '17px' }}>
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
                                </tr>
                            </thead>
                            <tbody>
                                {issues.map((i) => {
                                    const priorityBadge = getPriorityStyle(i.issuePriority);

                                    return (
                                        <tr key={i.issueId}>
                                            {/* 1. ID */}
                                            <td>
                                                <strong>{i.issueId}</strong>
                                            </td>

                                            {/* 2. Sorun Tanımı */}
                                            <td>
                                                <div className="font-medium" style={{ fontSize: '17px' }}>{i.issueTitle || '-'}</div>
                                            </td>

                                            {/* 3. Öncelik (Rozet boyutu 15px yapıldı) */}
                                            <td>
                                                <span style={{ 
                                                    backgroundColor: priorityBadge.bg, 
                                                    color: priorityBadge.text,
                                                    padding: '6px 12px', 
                                                    borderRadius: '12px', 
                                                    fontSize: '15px', 
                                                    fontWeight: 'bold',
                                                    display: 'inline-block'
                                                }}>
                                                    {i.issuePriority || 'Belirtilmemiş'}
                                                </span>
                                            </td>

                                            {/* 4. Etki */}
                                            <td>
                                                <strong>{i.issueImpact || '-'}</strong>
                                            </td>

                                            {/* 5. Durum */}
                                            <td>{i.issueStatus || '-'}</td>

                                            {/* 6. Sorumlu (User ID 15px yapıldı) */}
                                            <td>
                                                <div>{i.issueOwnerFullName || i.issueOwnerUser?.fullName || 'Atanmamış'}</div>
                                                {i.issueOwnerUserId && (
                                                    <small style={{ color: '#6b7280', fontSize: '15px' }}>User ID: {i.issueOwnerUserId}</small>
                                                )}
                                            </td>

                                            {/* 7. Kök Neden & Çözüm (15px/16px seviyesine çıkarıldı) */}
                                            <td>
                                                <div style={{ fontSize: '15px', marginBottom: '6px' }}>
                                                    <strong>Kök Neden:</strong> {i.rootCause || '-'}
                                                </div>
                                                <div style={{ fontSize: '15px', color: '#16a34a' }}>
                                                    <strong>Çözüm:</strong> {i.issueResolution || '-'}
                                                </div>
                                            </td>

                                            {/* 8. Tarih Detayları (15px seviyesine çıkarıldı) */}
                                            <td>
                                                <div style={{ fontSize: '15px', lineHeight: '1.6' }}>
                                                    <div><strong>Açılış:</strong> {formatDate(i.openedDate)}</div>
                                                    <div><strong>Hedef:</strong> {formatDate(i.issueDueDate)}</div>
                                                    <div><strong>Kapanış:</strong> {formatDate(i.closedDate)}</div>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>
        </div>
    );
}

export default ProblemsPage;