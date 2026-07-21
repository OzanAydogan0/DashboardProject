import api from './api';

export const projectService = {
  // SCR-02: Portföy Dashboardu KPI ve Grafik Verileri (C# GET /dashboard ile eşleşti)
  getPortfolioDashboard: async (filters = {}) => {
    const response = await api.get('/dashboard', { params: filters });
    return response.data; // List<DashboardSummaryDto> dizisi döner
  },

  // SCR-03: Proje Listesi
  getProjects: async (params = {}) => {
    const response = await api.get('/projects', { params });
    return response.data;
  },

  // SCR-04: Tek Proje Detay Özet Verisi
  getProjectDashboard: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/dashboard`);
    return response.data;
  },

  // Proje EVM Finansal Geçmiş Verisi (C# GET /dashboard/projects/{id}/evm)
  getProjectEvm: async (projectId) => {
    const response = await api.get(`/dashboard/projects/${projectId}/evm`);
    return response.data;
  },

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

  // Excel Dışa Aktarımı
  exportProjectsExcel: async (filters = {}) => {
    const response = await api.get('/projects/export.xlsx', {
      params: filters,
      responseType: 'blob',
    });
    return response.data;
  },

  // ---------------------------------------------------------
  // TEK PROJE DETAYI: ALT SEKME VERİLERİ (GET & POST)
  // ---------------------------------------------------------

  // PİR Dönemleri (Raporlar)
  getProjectReports: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/reports`);
    return response.data;
  },
  createProjectReport: async (projectId, reportData) => {
    const response = await api.post(`/projects/${projectId}/reports`, reportData);
    return response.data;
  },

  // Kilometre Taşları (Milestones)
  getProjectMilestones: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/milestones`);
    return response.data;
  },
  createProjectMilestone: async (projectId, milestoneData) => {
    const response = await api.post(`/projects/${projectId}/milestones`, milestoneData);
    return response.data;
  },

  // Riskler (Risks)
  getProjectRisks: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/risks`);
    return response.data;
  },
  createProjectRisk: async (projectId, riskData) => {
    const response = await api.post(`/projects/${projectId}/risks`, riskData);
    return response.data;
  },

  // Sorunlar (Issues)
  getProjectIssues: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/issues`);
    return response.data;
  },
  createProjectIssue: async (projectId, issueData) => {
    const response = await api.post(`/projects/${projectId}/issues`, issueData);
    return response.data;
  },

  // Aksiyonlar (Actions)
  getProjectActions: async (projectId) => {
    const response = await api.get(`/projects/${projectId}/actions`);
    return response.data;
  },
  createProjectAction: async (projectId, actionData) => {
    const response = await api.post(`/projects/${projectId}/actions`, actionData);
    return response.data;
  }
};