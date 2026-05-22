import client from '../api/client';

export const roomService = {
  getAll: async () => {
    const response = await client.get('/classroom');
    return response.data;
  },

  getById: async (id) => {
    const response = await client.get(`/classroom/${id}`);
    return response.data;
  },

  create: async (roomData) => {
    const response = await client.post('/classroom', roomData);
    return response.data;
  },

  update: async (id, roomData) => {
    const response = await client.put(`/classroom/${id}`, roomData);
    return response.data;
  },

  delete: async (id) => {
    await client.delete(`/classroom/${id}`);
  }
};
