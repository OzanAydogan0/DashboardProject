import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';

/**
 * Proje Aksiyon Kayıtları (Project Actions) Tablo Bileşeni
 * Backend C# LINQ sorgusundaki 11 alanın tamamını büyük punto ve düzenli formatta görüntüler.
 */
function ProjectActionsPage() {
    // 1. URL'den proje ID'sini alıyoruz
    const { id: projectId } = useParams();

    // 2. State tanımlamaları
    const [actions, setActions] = useState([]);
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
        const fetchActions = async () => {
            try {
                const token = localStorage.getItem('token');

                const response = await fetch(`http://localhost:5074/projects/${projectId}/actions`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    }
                });

                if (response.status === 401) throw new Error("Oturum süreniz dolmuş, lütfen tekrar giriş yapın.");
                if (response.status === 403) throw new Error("Bu projenin aksiyon verilerini görmeye yetkiniz yok!");
                if (!response.ok) throw new Error("Aksiyon kayıtları yüklenirken bir hata oluştu.");

                const data = await response.json();
                setActions(data);

            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        if (projectId) {
            fetchActions();
        }
    }, [projectId]);

    // 6. Yüklenme ve Hata Durumları
    if (loading) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px', fontSize: '18px' }}>Aksiyon kayıtları yükleniyor...</p></div>;
    if (error) return <div className="tab-content-wrapper fade-in"><p style={{ padding: '20px', color: 'red', fontSize: '18px' }}>Hata: {error}</p></div>;

    // 7. Ana Ekran ve Büyütülmüş Puntolu Aksiyon Tablosu
    return (
        <div className="tab-content-wrapper fade-in">
            <div className="dashboard-card shadow-card">
                
                <div className="card-header">
                    <h2>Proje Aksiyonları (Actions)</h2>
                </div>

                <div className="table-responsive">
                    {actions.length === 0 ? (
                        <p style={{ padding: '20px', textAlign: 'center', fontSize: '18px' }}>Bu projeye ait henüz bir aksiyon kaydı bulunmuyor.</p>
                    ) : (
                        <table className="modern-table" style={{ width: '100%', textAlign: 'left', fontSize: '17px' }}>
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Aksiyon Tanımı</th>
                                    <th>Kaynak / Ref</th>
                                    <th>Öncelik</th>
                                    <th>Durum / İlerleme</th>
                                    <th>Sorumlu</th>
                                    <th>Tarih Detayları</th>
                                </tr>
                            </thead>
                            <tbody>
                                {actions.map((a) => {
                                    const priorityBadge = getPriorityStyle(a.actionPriority);

                                    return (
                                        <tr key={a.actionId}>
                                            {/* 1. ActionId */}
                                            <td>
                                                <strong>{a.actionId}</strong>
                                            </td>

                                            {/* 2. ActionDescription */}
                                            <td>
                                                <div className="font-medium" style={{ fontSize: '17px' }}>
                                                    {a.actionDescription || '-'}
                                                </div>
                                            </td>

                                            {/* 3. SourceType & SourceReference */}
                                            <td>
                                                <div>{a.sourceType || '-'}</div>
                                                {a.sourceReference && (
                                                    <small style={{ color: '#6b7280', fontSize: '15px' }}>
                                                        Ref: {a.sourceReference}
                                                    </small>
                                                )}
                                            </td>

                                            {/* 4. ActionPriority */}
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
                                                    {a.actionPriority || 'Belirtilmemiş'}
                                                </span>
                                            </td>

                                            {/* 5. ActionStatus & ActionProgress */}
                                            <td>
                                                <div>{a.actionStatus || '-'}</div>
                                                {a.actionProgress !== undefined && a.actionProgress !== null && (
                                                    <div style={{ fontSize: '15px', color: '#2563eb', fontWeight: 'bold', marginTop: '4px' }}>
                                                        İlerleme: %{a.actionProgress}
                                                    </div>
                                                )}
                                            </td>

                                            {/* 6. ActionOwnerUser FullName & ActionOwnerUserId */}
                                            <td>
                                                <div>
                                                    {a.actionOwnerFullName || a.actionOwnerUser?.fullName || 'Atanmamış'}
                                                </div>
                                                {a.actionOwnerUserId && (
                                                    <small style={{ color: '#6b7280', fontSize: '15px' }}>
                                                        User ID: {a.actionOwnerUserId}
                                                    </small>
                                                )}
                                            </td>

                                            {/* 7. ActionDueDate & CompletedDate */}
                                            <td>
                                                <div style={{ fontSize: '15px', lineHeight: '1.6' }}>
                                                    <div><strong>Hedef:</strong> {formatDate(a.actionDueDate)}</div>
                                                    <div><strong>Tamamlanma:</strong> {formatDate(a.completedDate)}</div>
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

export default ProjectActionsPage;