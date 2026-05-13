import client from '../api/client';

/**
 * Enrollment Service
 * Handles all course registration and enrollment tracking logic.
 */
export const enrollmentService = {
  /**
   * Fetches all enrollments. 
   * Supports optional filters like sectionId or studentId.
   */
  async getAll(params = {}) {
    const response = await client.get('/enrollment', { params });
    return response.data;
  },

  /**
   * Fetches a specific enrollment by its ID.
   */
  async getById(id) {
    const response = await client.get(`/enrollment/${id}`);
    return response.data;
  },

  /**
   * Student Enrollment: Adds a student to a specific course section.
   */
  async enroll(sectionId) {
    const response = await client.post('/enrollment', { sectionId });
    return response.data;
  },

  /**
   * Updates an enrollment status (e.g., Active, Dropped, Completed).
   */
  async updateStatus(id, status) {
    const response = await client.put(`/enrollment/${id}/status`, { status });
    return response.data;
  }
};
