import client from '../api/client';

export const userService = {
  async getAll() {
    const response = await client.get('/account/users');
    return response.data;
  },
  async deleteUser(id) {
    await client.delete(`/account/users/${id}`);
  },
  async updateRole(id, role) {
    const response = await client.put(`/account/users/${id}/role`, { role });
    return response.data;
  }
};
