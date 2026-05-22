import React, { useState } from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { 
  LayoutDashboard, 
  BookOpen, 
  CalendarDays, 
  Building2, 
  Menu, 
  BookMarked,
  LayoutGrid,
  Users,
  ClipboardList,
  GraduationCap
} from 'lucide-react';

/**
 * DashboardLayout
 * Renders a fully role-aware sidebar navigation.
 * Role is read as a string from localStorage: "Admin", "Instructor", "Student"
 */
export default function DashboardLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/login');
  };

  const raw = localStorage.getItem('user') || '{}';
  const user = (() => { try { return JSON.parse(raw); } catch { return {}; } })();
  const userName = user.firstName || 'User';
  const role = user.role; // "Admin" | "Instructor" | "Student"

  const getNavItems = () => {
    if (role === 'Admin') {
      return [
        { name: 'Overview',                path: '/dashboard',                          icon: LayoutDashboard },
        { name: 'Master Schedule',         path: '/dashboard/master-schedule',          icon: LayoutGrid },
        { name: 'Room Management',         path: '/dashboard/rooms',                    icon: Building2 },
        { name: 'Course Catalog',          path: '/dashboard/courses',                  icon: BookOpen },
        { name: 'Manage Sections',         path: '/dashboard/sections',                 icon: ClipboardList },
        { name: 'Manage Users',            path: '/dashboard/users',                    icon: Users },
        { name: 'Instructor Availability', path: '/dashboard/instructor-availability',  icon: CalendarDays },
      ];
    }

    if (role === 'Instructor') {
      return [
        { name: 'Overview',         path: '/dashboard',                  icon: LayoutDashboard },
        { name: 'My Classes',       path: '/dashboard/sections',         icon: GraduationCap },
        { name: 'My Availability',  path: '/dashboard/availability',     icon: CalendarDays },
      ];
    }

    // Student
    return [
      { name: 'Overview',     path: '/dashboard',              icon: LayoutDashboard },
      { name: 'My Schedule',  path: '/dashboard/schedule',     icon: CalendarDays },
      { name: 'My Courses',   path: '/dashboard/my-courses',   icon: GraduationCap },
      { name: 'Courses',      path: '/dashboard/courses',      icon: BookOpen },
      { name: 'Enroll',       path: '/dashboard/sections',     icon: BookMarked },
    ];
  };

  const navItems = getNavItems();

  return (
    <div className="dash-layout">
      <aside className={`dash-sidebar ${isSidebarOpen ? 'dash-sidebar-expanded' : 'dash-sidebar-collapsed'}`}>
        <div className="dash-sidebar-header">
          <div className="dash-logo-box">
            <BookMarked className="dash-logo-icon" />
          </div>
          {isSidebarOpen && <span className="dash-brand-text">Scheduler</span>}
        </div>

        <nav className="dash-sidebar-nav">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = item.path === '/dashboard'
              ? location.pathname === item.path
              : location.pathname.startsWith(item.path);
            return (
              <Link
                key={item.name}
                to={item.path}
                className={`dash-nav-item ${isActive ? 'active' : ''}`}
                title={!isSidebarOpen ? item.name : undefined}
              >
                <Icon className="dash-nav-icon" />
                {isSidebarOpen && <span>{item.name}</span>}
              </Link>
            );
          })}
        </nav>

        {isSidebarOpen && (
          <div className="dash-sidebar-footer">
            <span className="dash-label-subtle">{role}</span>
            <span className="dash-label-subtle">{user.email}</span>
          </div>
        )}
      </aside>

      <div className="dash-main">
        <header className="dash-header">
          <button 
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            className="dash-menu-button"
            aria-label="Toggle Sidebar"
          >
            <Menu className="dash-menu-icon" />
          </button>
          
          <div className="dash-user-controls">
            <span className="dash-greeting">Welcome, {userName}</span>
            <button onClick={handleLogout} className="dash-logout-btn">
              Logout
            </button>
          </div>
        </header>

        <main className="dash-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
