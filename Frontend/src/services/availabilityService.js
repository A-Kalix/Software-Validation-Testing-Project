import client from '../api/client';

/**
 * Availability Service
 * Handles instructor teaching slot preferences.
 */
export const availabilityService = {
  /**
   * Get all availability slots for the current instructor
   */
  getMyAvailability: async () => {
    const response = await client.get('/Availability');
    return response.data;
  },

  /**
   * Bulk update availability slots
   * @param {Array} availabilities - List of {dayOfWeek, startTime, endTime, isPreferred}
   */
  updateBulk: async (availabilities) => {
    const response = await client.post('/Availability/bulk', { availabilities });
    return response.data;
  }
};
