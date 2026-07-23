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

  // SCR-03: Proje Listesi (GET /projects)
  getProjects: async (params = {}) => {
    const response = await api.get('/projects', { params });
    return response.data;
  },

  // Tek Proje Detayı (GET /projects/{id})
  getProjectById: async (id) => {
    const response = await api.get(`/projects/${id}`);
    return response.data;
  },

  // ➕ Yeni Proje Ekleme (POST /projects)
  createProject: async (projectData) => {
    const response = await api.post('/projects', projectData);
    return response.data;
  },

  // ✏️ Proje Güncelleme (PATCH /projects/{id})
  updateProject: async (projectId, projectData) => {
    const response = await api.patch(`/projects/${projectId}`, projectData);
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

  // SCR-04: Tek Proje Detay Özet Verisi
  getProjectDashboard: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/dashboard`);
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
      responseType: 'blob', // Dosya indirme işlemleri için gereklidir
    });
    return response.data;
  },

  // 📑 RAPOR OLUŞTURMA VE DIŞA AKTARMA (GÜNCELLENDİ VE GÜVENLİ HALE GETİRİLDİ)
  generateReport: async (projectId, format = 'pdf', startDate, endDate) => {
    const pirsResponse = await api.get(`/projects/${projectId}/pirs`);
    let pirs = pirsResponse.data;

    if (!pirs || pirs.length === 0) {
      throw new Error("Bu projeye ait henüz oluşturulmuş bir PIR raporu bulunamadı.");
    }

    // 💡 İYİLEŞTİRME 1: Raporları en yeni tarihten en eskiye doğru sıralıyoruz
    pirs.sort((a, b) => {
      const dateA = new Date(a.reportDate || a.createdAt || 0);
      const dateB = new Date(b.reportDate || b.createdAt || 0);
      return dateB - dateA; // Büyük tarihten küçüğe sırala
    });

    // 💡 İYİLEŞTİRME 2: Başlangıç Tarihi Filtresi
    if (startDate) {
      const start = new Date(startDate);
      start.setHours(0, 0, 0, 0);
      pirs = pirs.filter(p => p.reportDate && new Date(p.reportDate) >= start);
    }
    
    // 💡 İYİLEŞTİRME 3: Bitiş Tarihi Filtresi
    if (endDate) {
      const end = new Date(endDate);
      end.setHours(23, 59, 59, 999);
      pirs = pirs.filter(p => p.reportDate && new Date(p.reportDate) <= end);
    }

    if (pirs.length === 0) {
      throw new Error("Seçilen tarih aralığında bu projeye ait bir rapor bulunamadı. Lütfen farklı tarihler seçin.");
    }

    // Artık pirs[0] elemanının EN YENİ rapor olduğundan %100 eminiz.
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

  // --- PIR Dönemleri (Raporlar) ---
  getProjectReports: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/pirs`);
    return response.data;
  },

  createProjectReport: async (projectId, reportData) => {
    const payload = { ...reportData, projectId };
    const response = await api.post(`/pirs`, payload);
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
  // 🌟 MÜŞTERİ & SİSTEM & KİLOMETRE TAŞLARI (MILESTONES)
  // =========================================================

  getCustomers: async () => {
    const response = await api.get('/customers'); 
    return response.data;
  },

  getPrograms: async () => {
    const response = await api.get('/programs');
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
  },

  // --- Kilometre Taşları (Milestones) ---
  getProjectMilestones: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/milestones`);
    return response.data;
  },

  createProjectMilestone: async (projectId, milestoneData) => {
    const response = await api.post(`/projects/${projectId}/milestones`, milestoneData);
    return response.data;
  },

  deleteProjectMilestone: async (milestoneId) => {
    const response = await api.delete(`/milestones/${milestoneId}`);
    return response.data;
  }
};