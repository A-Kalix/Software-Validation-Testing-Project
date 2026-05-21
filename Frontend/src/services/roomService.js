import axios from 'axios';

const API_URL = 'http://localhost:5001/api/Classroom';

const getAuthHeader = () => {
  const user = JSON.parse(localStorage.getItem('user'));
  return user?.token ? { Authorization: `Bearer ${user.token}` } : {};
};

export const roomService = {
  getAll: async () => {
    const response = await axios.get(API_URL, { headers: getAuthHeader() });
    return response.data;
  },

  getById: async (id) => {
    const response = await axios.get(`${API_URL}/${id}`, { headers: getAuthHeader() });
    return response.data;
  },

  create: async (roomData) => {
    const response = await axios.post(API_URL, roomData, { headers: getAuthHeader() });
    return response.data;
  },

  update: async (id, roomData) => {
    await axios.put(`${API_URL}/${id}`, roomData, { headers: getAuthHeader() });
  },

  delete: async (id) => {
    await axios.delete(`${API_URL}/${id}`, { headers: getAuthHeader() });
  }
};
