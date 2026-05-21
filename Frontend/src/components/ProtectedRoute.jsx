import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';

export default function ProtectedRoute({ allowedRoles }) {
  // Simple check for token presence.
  const token = localStorage.getItem('token');
  const userString = localStorage.getItem('user');

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  // If we need role-based access later, we can parse the user string
  if (allowedRoles && userString) {
    try {
      const user = JSON.parse(userString);
      if (!allowedRoles.includes(user.role)) {
        return <Navigate to="/dashboard" replace />;
      }
    } catch (e) {
      // ignore
    }
  }

  return <Outlet />;
}
