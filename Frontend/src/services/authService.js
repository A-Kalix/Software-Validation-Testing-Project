import client from '../api/client';
import { API_ENDPOINTS } from '../utils/constants';

/**
 * Authentication Service
 * Encapsulates all auth-related API calls to keep components lean and focused on UI.
 */
const authService = {
  /**
   * Logs in a user and returns the AuthResponseDTO
   */
  login: async (credentials) => {
    const response = await client.post(API_ENDPOINTS.LOGIN, credentials);
    return response.data;
  },

  /**
   * Registers a new user and returns the UserResponseDTO
   */
  register: async (userData) => {
    const response = await client.post(API_ENDPOINTS.REGISTER, userData);
    return response.data;
  },

  /**
   * Centralized token management can be added here (localStorage, etc.)
   */
  handleAuthSuccess: (authData) => {
    if (authData.token) {
      localStorage.setItem('token', authData.token);
      localStorage.setItem('user', JSON.stringify(authData.user));
    }
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
};

export default authService;
