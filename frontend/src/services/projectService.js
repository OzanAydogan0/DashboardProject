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

// =========================================================
  // 📥 EXCEL İLE İÇE AKTARMA (IMPORT)
  // =========================================================
  // projectService.js
importProjectsExcel: (formData) => {
  return api.post('/projects/import', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
},

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
  generateReport: async (projectId, format = 'pdf', selectedReportId = null) => {
    const pirsResponse = await api.get(`/projects/${projectId}/pirs`);
    const pirs = Array.isArray(pirsResponse.data) ? pirsResponse.data : [];

    if (!pirs.length) {
      throw new Error("Bu projeye ait henüz oluşturulmuş bir PIR raporu bulunamadı.");
    }

    const selectedPir = selectedReportId
      ? pirs.find((pir) => pir.pirReportId === selectedReportId || pir.id === selectedReportId || pir.period === selectedReportId)
      : null;

    if (!selectedPir) {
      throw new Error("Seçilen rapor dönemi bulunamadı.");
    }

    const pirId = selectedPir.pirReportId || selectedPir.id;

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

  updateProjectReport: async (reportId, reportData) => {
    const response = await api.patch(`/pirs/${reportId}`, reportData);
    return response.data;
  },

  deleteProjectReport: async (reportId) => {
    const response = await api.delete(`/pirs/${reportId}`);
    return response.data;
  },

  createProjectRisk: async (projectId, riskData) => {
    const payload = { ...riskData, projectId };
    const response = await api.post(`/risks`, payload);
    return response.data;
  },

  importProjectRisksExcel: (projectId, formData) => {
    return api.post(`/projects/${projectId}/risks/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
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

  createCustomer: async (customerData) => {
    const response = await api.post('/customers', customerData);
    return response.data;
  },

  updateCustomer: async (customerId, customerData) => {
    const response = await api.patch(`/customers/${customerId}`, customerData);
    return response.data;
  },

  deleteCustomer: async (customerId) => {
    const response = await api.delete(`/customers/${customerId}`);
    return response.data;
  },

  getPrograms: async () => {
    const response = await api.get('/programs');
    return response.data;
  },

  getUsers: async () => {
    const response = await api.get('/users');
    return response.data;
  },

  createUser: async (userData) => {
    const response = await api.post('/users', userData);
    return response.data;
  },

  updateUser: async (userId, userData) => {
    const response = await api.patch(`/users/${userId}`, userData);
    return response.data;
  },

  deleteUser: async (userId) => {
    const response = await api.delete(`/users/${userId}`);
    return response.data;
  },

  getAuditLogs: async () => {
    const response = await api.get('/audit-logs');
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

  deleteRisk: async (riskId) => {
    const response = await api.delete(`/risks/${riskId}`);
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
