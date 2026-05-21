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

  async delete(id) {
    await client.delete(`/section/${id}`);
  },

  async runScheduler(semester) {
    const response = await client.post(`/scheduling/run?semester=${semester}`);
    return response.data;
  },

  async publishSchedule(semester) {
    const response = await client.post(`/scheduling/publish?semester=${semester}`);
    return response.data;
  },

  async getSemesterStatus(semester) {
    const response = await client.get(`/scheduling/status?semester=${semester}`);
    return response.data;
  }
};
