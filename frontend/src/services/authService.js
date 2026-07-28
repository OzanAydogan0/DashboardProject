import api from './api';

export const authService = {
  login: async (email, password) => {
    const response = await api.post('/auth/login', { email, password });
    
    // Backend'den gelen token verisi 'token' veya 'access_token' olabilir
    const token = response.data.token || response.data.access_token;
    const user = {
      userId: response.data.userId,
      fullName: response.data.fullName,
      userRole: response.data.userRole,
    };
    
    if (token) {
      localStorage.setItem('token', token);
    }

    if (user.userId || user.fullName || user.userRole) {
      localStorage.setItem('user', JSON.stringify(user));
    }
    
    return response.data;
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
};