import client from '../api/client';

export const courseService = {
  async getAll(params = {}) {
    const response = await client.get('/course', { params });
    return response.data;
  },

  async getById(id) {
    const response = await client.get(`/course/${id}`);
    return response.data;
  },

  async create(data) {
    const response = await client.post('/course', data);
    return response.data;
  },

  async update(id, data) {
    const response = await client.put(`/course/${id}`, data);
    return response.data;
  },

  async delete(id) {
    await client.delete(`/course/${id}`);
  }
};
