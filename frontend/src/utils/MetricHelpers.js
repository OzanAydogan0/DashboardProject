/**
 * Metrik türüne ve değerine göre Yeşil / Sarı / Kırmızı renk stillerini döndürür.
 * 
 * @param {string} metricType - 'spi' | 'cpi' | 'finishVariance' | 'progressVariance' | 'riskScore'
 * @param {number} value - Metriğin sayısal değeri
 * @returns {object} { bg, text } stil nesnesi
 */
export const getMetricStatusStyle = (metricType, value) => {
    // Tanımsız veya sayı olmayan değerler için nötr gri stil
    if (value === null || value === undefined || isNaN(value)) {
        return { bg: '#f3f4f6', text: '#374151' };
    }

    const numValue = Number(value);

    // Renk Sabitleri
    const GREEN = { bg: '#d1fae5', text: '#065f46' };  // Yeşil (Başarılı / Düşük Risk)
    const YELLOW = { bg: '#fef3c7', text: '#92400e' }; // Sarı (Uyarı / Takip)
    const RED = { bg: '#fee2e2', text: '#991b1b' };    // Kırmızı (Kritik / Yüksek Risk)

    switch (metricType) {
        // 1. SPI & 2. CPI Kuralları (≥0.95 Yeşil, 0.85-0.94 Sarı, <0.85 Kırmızı)
        case 'spi':
        case 'cpi':
            if (numValue >= 0.95) return GREEN;
            if (numValue >= 0.85 && numValue <= 0.94) return YELLOW;
            return RED;

        // 3. Bitiş Sapması - Gün (≤15 Yeşil, 16-45 Sarı, >45 Kırmızı)
        case 'finishVariance':
            if (numValue <= 15) return GREEN;
            if (numValue >= 16 && numValue <= 45) return YELLOW;
            return RED;

        // 4. İlerleme Sapması - Puan (≥-5 Yeşil, -6 ile -15 Sarı, <-15 Kırmızı)
        case 'progressVariance':
            if (numValue >= -5) return GREEN;
            if (numValue >= -15 && numValue <= -6) return YELLOW;
            return RED;

        // 5. Risk Puanı (1-4 Yeşil, 5-15 Sarı, 16-25 Kırmızı)
        case 'riskScore':
            if (numValue >= 1 && numValue <= 4) return GREEN;
            if (numValue >= 5 && numValue <= 15) return YELLOW;
            if (numValue >= 16 && numValue <= 25) return RED;
            return RED;

        default:
            return { bg: '#f3f4f6', text: '#374151' };
    }
};