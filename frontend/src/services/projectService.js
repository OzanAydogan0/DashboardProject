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
  }
};