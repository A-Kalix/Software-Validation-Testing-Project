import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';

import DashboardLayout from './components/DashboardLayout';
import ProtectedRoute from './components/ProtectedRoute';
import Dashboard from './pages/Dashboard';
import Courses from './pages/Courses';
import Rooms from './pages/Rooms';
import Sections from './pages/Sections';
import Users from './pages/Users';
import MasterSchedule from './pages/MasterSchedule';
import InstructorAvailability from './pages/InstructorAvailability';
import MySchedule from './pages/MySchedule';

/**
 * Main Application Component
 * Handles global routing and layouts.
 */
function App() {
  return (
    <Router>
      <Routes>
        {/* Redirect root to login for now */}
        <Route path="/" element={<Navigate to="/login" replace />} />
        
        {/* Authentication Routes */}
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        {/* Protected Dashboard Routes */}
        <Route element={<ProtectedRoute><DashboardLayout /></ProtectedRoute>}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/courses" element={<Courses />} />
          <Route path="/rooms" element={<Rooms />} />
          <Route path="/sections" element={<Sections />} />
          <Route path="/users" element={<Users />} />
          <Route path="/master-schedule" element={<MasterSchedule />} />
          <Route path="/availability" element={<InstructorAvailability />} />
          <Route path="/my-schedule" element={<MySchedule />} />
        </Route>
        
        {/* Fallback route */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Router>
  );
}

export default App;