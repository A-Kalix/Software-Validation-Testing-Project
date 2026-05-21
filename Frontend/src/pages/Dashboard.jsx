import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  BookOpen, CalendarCheck, BookMarked, UserCheck, 
  Users, Building2, LayoutGrid, GraduationCap, 
  ClipboardList, Play, ArrowRight
} from 'lucide-react';
import { courseService } from '../services/courseService';
import { sectionService } from '../services/sectionService';
import { enrollmentService } from '../services/enrollmentService';

/**
 * Dashboard — Role-Aware Overview
 * Renders different stat cards and quick actions based on user role string.
 */
export default function Dashboard() {
  const raw = localStorage.getItem('user') || '{}';
  const user = (() => { try { return JSON.parse(raw); } catch { return {}; } })();
  const role = user.role; // "Admin" | "Instructor" | "Student"

  const [stats, setStats] = useState({ courses: 0, sections: 0, enrollments: 0 });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setIsLoading(true);
        const [coursesRes, sectionsRes, enrollmentsRes] = await Promise.all([
          courseService.getAll().catch(() => []),
          sectionService.getAll().catch(() => []),
          enrollmentService.getAll().catch(() => []),
        ]);
        setStats({
          courses: coursesRes?.length ?? 0,
          sections: sectionsRes?.length ?? 0,
          enrollments: enrollmentsRes?.length ?? 0,
        });
      } finally {
        setIsLoading(false);
      }
    };
    fetchStats();
  }, []);

  // ── Role: Admin ──────────────────────────────────────────────────────────
  if (role === 'Admin') {
    return (
      <div className="dash-container">
        <div>
          <h1 className="dash-title">Admin Control Panel</h1>
          <p className="dash-subtitle">Global university scheduling, rooms, courses, and users.</p>
        </div>

        <div className="dash-stats-grid">
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">Total Courses</h3>
                {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.courses}</p>}
              </div>
              <div className="dash-feature-icon-wrapper"><BookOpen className="dash-feature-icon" /></div>
            </div>
          </div>
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">Active Sections</h3>
                {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.sections}</p>}
              </div>
              <div className="dash-success-icon-wrapper"><ClipboardList className="dash-success-icon" /></div>
            </div>
          </div>
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">Total Enrollments</h3>
                {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.enrollments}</p>}
              </div>
              <div className="dash-accent-icon-wrapper"><Users className="dash-accent-icon" /></div>
            </div>
          </div>
        </div>

        <h2 className="dash-section-title">Quick Actions</h2>
        <div className="dash-btn-group">
          <Link to="/dashboard/master-schedule" className="dash-primary-btn">
            <Play className="dash-icon-primary" />
            Run Auto-Scheduler
          </Link>
          <Link to="/dashboard/rooms" className="dash-action-button">
            <Building2 className="dash-icon-primary" />
            Manage Rooms
          </Link>
          <Link to="/dashboard/courses" className="dash-action-button">
            <BookOpen className="dash-icon-primary" />
            Manage Courses
          </Link>
          <Link to="/dashboard/sections" className="dash-action-button">
            <ClipboardList className="dash-icon-primary" />
            Manage Sections
          </Link>
          <Link to="/dashboard/users" className="dash-action-button">
            <Users className="dash-icon-primary" />
            Manage Users
          </Link>
        </div>
      </div>
    );
  }

  // ── Role: Instructor ─────────────────────────────────────────────────────
  if (role === 'Instructor') {
    return (
      <div className="dash-container">
        <div>
          <h1 className="dash-title">Instructor Portal</h1>
          <p className="dash-subtitle">Manage your availability and view your assigned classes.</p>
        </div>

        <div className="dash-stats-grid">
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">My Sections</h3>
                {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.sections}</p>}
              </div>
              <div className="dash-feature-icon-wrapper"><GraduationCap className="dash-feature-icon" /></div>
            </div>
          </div>
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">Enrolled Students</h3>
                {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.enrollments}</p>}
              </div>
              <div className="dash-success-icon-wrapper"><Users className="dash-success-icon" /></div>
            </div>
          </div>
          <div className="dash-card">
            <div className="dash-card-header-layout">
              <div>
                <h3 className="dash-card-title">Status</h3>
                <p className="dash-card-value">Active</p>
              </div>
              <div className="dash-accent-icon-wrapper"><UserCheck className="dash-accent-icon" /></div>
            </div>
          </div>
        </div>

        <h2 className="dash-section-title">Quick Actions</h2>
        <div className="dash-btn-group">
          <Link to="/dashboard/sections" className="dash-primary-btn">
            <GraduationCap className="dash-icon-primary" />
            View My Classes
          </Link>
          <Link to="/dashboard/availability" className="dash-action-button">
            <CalendarCheck className="dash-icon-primary" />
            Update Availability
          </Link>
        </div>
      </div>
    );
  }

  // ── Role: Student (default) ──────────────────────────────────────────────
  return (
    <div className="dash-container">
      <div>
        <h1 className="dash-title">Welcome back, {user.firstName || 'Student'}</h1>
        <p className="dash-subtitle">Your academic schedule and course enrollments.</p>
      </div>

      <div className="dash-stats-grid">
        <div className="dash-card">
          <div className="dash-card-header-layout">
            <div>
              <h3 className="dash-card-title">Available Courses</h3>
              {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.courses}</p>}
            </div>
            <div className="dash-feature-icon-wrapper"><BookOpen className="dash-feature-icon" /></div>
          </div>
        </div>
        <div className="dash-card">
          <div className="dash-card-header-layout">
            <div>
              <h3 className="dash-card-title">Open Sections</h3>
              {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.sections}</p>}
            </div>
            <div className="dash-success-icon-wrapper"><CalendarCheck className="dash-success-icon" /></div>
          </div>
        </div>
        <div className="dash-card">
          <div className="dash-card-header-layout">
            <div>
              <h3 className="dash-card-title">My Enrollments</h3>
              {isLoading ? <div className="dash-skeleton" /> : <p className="dash-card-value">{stats.enrollments}</p>}
            </div>
            <div className="dash-accent-icon-wrapper"><BookMarked className="dash-accent-icon" /></div>
          </div>
        </div>
      </div>

      <h2 className="dash-section-title">Quick Actions</h2>
      <div className="dash-btn-group">
        <Link to="/dashboard/sections" className="dash-primary-btn">
          <BookMarked className="dash-icon-primary" />
          Enroll in Classes
        </Link>
        <Link to="/dashboard/schedule" className="dash-action-button">
          <CalendarCheck className="dash-icon-primary" />
          View My Schedule
        </Link>
        <Link to="/dashboard/courses" className="dash-action-button">
          <BookOpen className="dash-icon-primary" />
          Browse Courses
        </Link>
      </div>
    </div>
  );
}
