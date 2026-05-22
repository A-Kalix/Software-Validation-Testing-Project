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
   * Bulk-replace all availability slots for the current instructor.
   * @param {Array} availabilities - List of {dayOfWeek:int, startTime:'HH:mm', endTime:'HH:mm', isPreferred:bool}
   */
  updateBulk: async (availabilities) => {
    // Normalise to HH:mm (pad hour if needed), drop any extra fields like 'id'
    const normalized = availabilities.map(a => ({
      dayOfWeek: Number(a.dayOfWeek),
      startTime: String(a.startTime).substring(0, 5).padStart(5, '0'),  // "9:00" → "09:00"
      endTime:   String(a.endTime).substring(0, 5).padStart(5, '0'),
      isPreferred: a.isPreferred ?? true,
    }));
    const response = await client.post('/Availability/bulk', { availabilities: normalized });
    return response.data;
  }
};
