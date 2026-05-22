import client from '../api/client';

export const sectionService = {
  async getAll(params = {}) {
    const response = await client.get('/section', { params });
    return response.data;
  },

  async getById(id) {
    const response = await client.get(`/section/${id}`);
    return response.data;
  },

  async create(data) {
    const response = await client.post('/section', data);
    return response.data;
  },

  async update(id, data) {
    const response = await client.put(`/section/${id}`, data);
    return response.data;
  },

  async delete(id) {
    await client.delete(`/section/${id}`);
  },

  async runScheduler(semester) {
    const encoded = encodeURIComponent(semester);
    const response = await client.post(`/scheduling/run?semester=${encoded}`);
    return response.data;
  },

  async publishSchedule(semester) {
    const encoded = encodeURIComponent(semester);
    const response = await client.post(`/scheduling/publish?semester=${encoded}`);
    return response.data;
  },

  async getSemesterStatus(semester) {
    const encoded = encodeURIComponent(semester);
    const response = await client.get(`/scheduling/status?semester=${encoded}`);
    return response.data;
  }
};
