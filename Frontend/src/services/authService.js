import client from '../api/client';

/**
 * Authentication Service
 * Encapsulates all auth-related API calls to keep components lean and focused on UI.
 */
export const authService = {
  async login(credentials) {
    const response = await client.post('/account/login', credentials);
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data.user));
    }
    return response.data;
  },

  async register(userData) {
    const response = await client.post('/account/register', userData);
    return response.data;
  },

  async getProfile() {
    const response = await client.get('/account/profile');
    return response.data;
  },

  async updateProfile(userData) {
    const response = await client.put('/account/profile', userData);
    localStorage.setItem('user', JSON.stringify(response.data));
    return response.data;
  },

  async changePassword(passwordData) {
    await client.post('/account/change-password', passwordData);
  },

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/login';
  },

  getCurrentUser() {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }
};

export default authService;
