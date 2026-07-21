import api from './api';

export const authService = {
  login: async (email, password) => {
    const response = await api.post('/auth/login', { email, password });
    
    // Backend'den gelen token verisi 'token' veya 'access_token' olabilir
    const token = response.data.token || response.data.access_token;
    
    if (token) {
      localStorage.setItem('token', token);
    }
    
    return response.data;
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
};