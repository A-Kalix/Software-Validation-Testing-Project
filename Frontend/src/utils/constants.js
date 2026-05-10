/**
 * Application Constants
 * Centralized store for static data to maintain consistency across the app.
 */

export const DEPARTMENTS = [
  { value: '11111111-1111-1111-1111-111111111111', label: 'Computer Science' },
  { value: '22222222-2222-2222-2222-222222222222', label: 'Software Engineering' },
  { value: '33333333-3333-3333-3333-333333333333', label: 'Data Science' }
];

export const USER_ROLES = [
  { value: '0', label: 'Student' },
  { value: '1', label: 'Instructor' }
];

export const API_ENDPOINTS = {
  LOGIN: '/Account/login',
  REGISTER: '/Account/register'
};
