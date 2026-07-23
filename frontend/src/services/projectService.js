import api from './api';

export const projectService = {
  // =========================================================
  // 📊 DASHBOARD & PROJE GENEL VERİLERİ
  // =========================================================

  // SCR-02: Portföy Dashboardu KPI ve Grafik Verileri
  getPortfolioDashboard: async (filters = {}) => {
    const response = await api.get('/dashboard', { params: filters });
    return response.data;
  },

  // SCR-03: Proje Listesi
  getProjects: async (params = {}) => {
    const response = await api.get('/projects', { params });
    return response.data;
  },

// --- Riskler (Risks) ---
getAllRisks: async () => {
  const response = await api.get('/risks');
  return response.data;
},
getProjectRisks: async (projectId) => {
  const response = await api.get(`/projects/${projectId}/risks`);
  return response.data;
},

// --- Aksiyonlar (Actions) ---
getAllActions: async () => {
  const response = await api.get('/actions');
  return response.data;
},
getProjectActions: async (projectId) => {
  const response = await api.get(`/projects/${projectId}/actions`);
  return response.data;
},

  // SCR-04: Tek Proje Detay Özet Verisi (Eski kodundaki yapı korundu)
  getProjectDashboard: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/dashboard`);
    return response.data;
  },


  getProjectById: async (id) => {
    const response = await api.get(`/projects/${id}`);
    return response.data;
  },
<<<<<<< HEAD
//customers endpointi ekleniyor
  getCustomers: async () => {
    // Kendi axios veya fetch yapınıza göre uyarlayın
    const response = await api.get('/customers'); 
    return response.data;
  },
  // Proje EVM Finansal Geçmiş Verisi
  getProjectEvm: async (projectId) => {
    const response = await api.get(`/dashboard/projects/${projectId}/evm`);
    return response.data;
  },
=======
>>>>>>> 418d8d323c0d5de88a45f3717f503e03e9df3caa


  // Yeni Proje Ekleme
  createProject: async (projectData) => {
    const response = await api.post('/projects', projectData);
    return response.data;
  },

  // Proje Güncelleme
  updateProject: async (projectId, projectData) => {
    const response = await api.patch(`/projects/${projectId}`, projectData);
    return response.data;
  },

  // =========================================================
  // 📈 EVM & RAPORLAMA & DIŞA AKTARIM
  // =========================================================

  getProjectEvm: async (projectId) => {
    const response = await api.get(`/dashboard/projects/${projectId}/evm`);
    return response.data;
  },

  getEvmRecords: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/evm-records`);
    return response.data;
  },

  exportProjectsExcel: async (filters = {}) => {
    const response = await api.get('/projects/export.xlsx', {
      params: filters,
      responseType: 'blob',
    });
    return response.data;
  },

  // Eski kodundaki detaylı rapor alma fonksiyonu birebir korundu
  generateReport: async (projectId, format = 'pdf', startDate, endDate) => {
    const pirsResponse = await api.get(`/projects/${projectId}/pirs`);
    let pirs = pirsResponse.data;

    if (!pirs || pirs.length === 0) {
      throw new Error("Bu projeye ait henüz oluşturulmuş bir PIR raporu bulunamadı.");
    }

    if (startDate) {
      const start = new Date(startDate);
      start.setHours(0, 0, 0, 0); // Günün başı
      pirs = pirs.filter(p => new Date(p.reportDate) >= start);
    }
    
    if (endDate) {
      const end = new Date(endDate);
      end.setHours(23, 59, 59, 999); // Günün sonu
      pirs = pirs.filter(p => new Date(p.reportDate) <= end);
    }

    if (pirs.length === 0) {
      throw new Error("Seçilen tarih aralığında bu projeye ait bir rapor bulunamadı. Lütfen farklı tarihler seçin.");
    }

    const latestPir = pirs[0]; 
    const pirId = latestPir.pirReportId || latestPir.id;

    const endpoint = format === 'excel' 
      ? `/pirs/${pirId}/export/excel` 
      : `/pirs/${pirId}/export/pdf`;

    const response = await api.get(endpoint, {
      responseType: 'blob'
    });

    return response.data;
  },

  // =========================================================
  // 📑 TEK PROJE DETAYI: ALT SEKME VERİLERİ (GET & POST)
  // =========================================================

  // --- PİR Dönemleri (Raporlar) ---
  getProjectReports: async (projectId) => {
    // Backend'de endpoint '/pirs' olarak geçtiği için URL güncellendi, fonksiyon adı korundu.
    const response = await api.get(`/projects/${projectId}/pirs`);
    return response.data;
  },
  createProjectReport: async (projectId, reportData) => {
    // Frontend parametreleri korundu, payload içine projectId eklendi.
    const payload = { ...reportData, projectId };
    const response = await api.post(`/pirs`, payload);
    return response.data;
  },

  // --- Kilometre Taşları (Milestones) ---
  getProjectMilestones: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/milestones`);
    return response.data;
  },
  createProjectMilestone: async (projectId, milestoneData) => {
    const payload = { ...milestoneData, projectId };
    const response = await api.post(`/milestones`, payload);
    return response.data;
  },

  
  createProjectRisk: async (projectId, riskData) => {
    const payload = { ...riskData, projectId };
    const response = await api.post(`/risks`, payload);
    return response.data;
  },

  // --- Sorunlar (Issues) ---
  getProjectIssues: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/issues`);
    return response.data;
  },
  createProjectIssue: async (projectId, issueData) => {
    const payload = { ...issueData, projectId };
    const response = await api.post(`/issues`, payload);
    return response.data;
  },

  
  createProjectAction: async (projectId, actionData) => {
    const payload = { ...actionData, projectId };
    const response = await api.post(`/actions`, payload);
    return response.data;
  },

  // =========================================================
  // 🌟 YENİ EKLENEN OPERASYONLAR (GÜNCELLEME, SİSTEM, KULLANICI)
  // (Bunlar eski kodunu bozmaz, ekstra özellik kazandırır)
  // =========================================================

  getCustomers: async () => {
    const response = await api.get('/customers'); 
    return response.data;
  },

  updateMilestone: async (milestoneId, milestoneData) => {
    const response = await api.patch(`/milestones/${milestoneId}`, milestoneData);
    return response.data;
  },

  updateRisk: async (riskId, riskData) => {
    const response = await api.patch(`/risks/${riskId}`, riskData);
    return response.data;
  },

  updateIssue: async (issueId, issueData) => {
    const response = await api.patch(`/issues/${issueId}`, issueData);
    return response.data;
  },

  updateAction: async (actionId, actionData) => {
    const response = await api.patch(`/actions/${actionId}`, actionData);
    return response.data;
  }
};