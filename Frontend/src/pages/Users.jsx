import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { Users, Shield, GraduationCap, BookOpen, Search } from 'lucide-react';
import client from '../api/client';
import { toast } from 'sonner';

/**
 * Users Management Page (Admin Only)
 * Lists all registered users and allows role management.
 */
const UsersPage = () => {
  const [users, setUsers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');

  const fetchUsers = async () => {
    try {
      setIsLoading(true);
      // Uses the existing GET /api/account/profile pattern but for all users
      const response = await client.get('/account/users');
      setUsers(response.data);
    } catch (e) {
      // Backend may not have /users list endpoint yet, show a clear message
      toast.error('User list endpoint not available. See note below.');
      setUsers([]);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { fetchUsers(); }, []);

  const roleLabel = (role) => {
    const map = { Admin: 'Admin', Instructor: 'Instructor', Student: 'Student' };
    return map[role] ?? role;
  };

  const roleIcon = (role) => {
    if (role === 'Admin') return <Shield style={{ width: '14px', height: '14px', color: '#818cf8' }} />;
    if (role === 'Instructor') return <GraduationCap style={{ width: '14px', height: '14px', color: '#34d399' }} />;
    return <BookOpen style={{ width: '14px', height: '14px', color: '#94a3b8' }} />;
  };

  const filtered = users.filter(u =>
    `${u.firstName} ${u.lastName} ${u.email}`.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Manage Users</h1>
          <p className="dash-subtitle">View all registered university accounts.</p>
        </div>
      </header>

      <div className="dash-card dash-section-spacer">
        <div className="dash-search-input-wrapper">
          <Search className="dash-search-icon" />
          <input
            type="text"
            placeholder="Search by name or email..."
            className="dash-search-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <table className="dash-table">
            <thead className="dash-table-head">
              <tr>
                <th className="dash-table-th">Name</th>
                <th className="dash-table-th">Email</th>
                <th className="dash-table-th">Department</th>
                <th className="dash-table-th">Role</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                [1,2,3,4,5].map(i => (
                  <tr key={i} className="dash-table-tr">
                    <td colSpan="4" className="dash-table-td">
                      <div className="dash-skeleton" style={{ height: '16px', width: '100%' }} />
                    </td>
                  </tr>
                ))
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan="4" className="dash-table-empty-notice">
                    {users.length === 0
                      ? 'User list API endpoint not implemented. Add GET /api/account/users to the backend.'
                      : 'No users match your search.'}
                  </td>
                </tr>
              ) : filtered.map(u => (
                <motion.tr key={u.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="dash-table-tr">
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <div className="dash-badge-instructor">
                        {u.firstName?.[0]}{u.lastName?.[0]}
                      </div>
                      <span className="dash-heading-item">{u.firstName} {u.lastName}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-data">{u.email}</span>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-primary">{u.departmentName}</span>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      {roleIcon(u.role)}
                      <span className="dash-label-primary">{roleLabel(u.role)}</span>
                    </div>
                  </td>
                </motion.tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default UsersPage;
