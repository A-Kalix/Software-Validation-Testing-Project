import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { 
  Clock, User, Building, CheckCircle2, XCircle, 
  GraduationCap, Trash2, Plus, Pencil, Users
} from 'lucide-react';
import { sectionService } from '../services/sectionService';
import { enrollmentService } from '../services/enrollmentService';
import { toast } from 'sonner';

/**
 * Sections Page — Fully Role-Aware
 * Admin:      Sees all sections in a management table (CRUD)
 * Instructor: Sees only their assigned sections and student rosters
 * Student:    Sees published sections with enroll/drop buttons
 */
const Sections = () => {
  const raw = localStorage.getItem('user') || '{}';
  const user = (() => { try { return JSON.parse(raw); } catch { return {}; } })();
  const role = user.role;

  if (role === 'Admin') return <AdminSectionsView />;
  if (role === 'Instructor') return <InstructorSectionsView user={user} />;
  return <StudentSectionsView />;
};

// ─── ADMIN VIEW ──────────────────────────────────────────────────────────────
const AdminSectionsView = () => {
  const [sections, setSections] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchSections = async () => {
    try {
      setIsLoading(true);
      const data = await sectionService.getAll();
      setSections(data);
    } catch {
      toast.error('Failed to load sections');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { fetchSections(); }, []);

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this section? This cannot be undone.')) return;
    try {
      await sectionService.delete(id);
      toast.success('Section deleted');
      setSections(prev => prev.filter(s => s.id !== id));
    } catch (e) {
      toast.error(e.response?.data?.message || 'Cannot delete section with active enrollments.');
    }
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Manage Sections</h1>
          <p className="dash-subtitle">All course sections — create, edit, and delete. Use the Master Schedule for bulk automation.</p>
        </div>
      </header>

      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <table className="dash-table">
            <thead className="dash-table-head">
              <tr>
                <th className="dash-table-th">Course</th>
                <th className="dash-table-th">Instructor</th>
                <th className="dash-table-th">Schedule</th>
                <th className="dash-table-th">Room</th>
                <th className="dash-table-th">Semester</th>
                <th className="dash-table-th">Status</th>
                <th className="dash-table-th">Enrolled</th>
                <th className="dash-table-th dash-table-align-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                [1,2,3].map(i => (
                  <tr key={i} className="dash-table-tr">
                    <td colSpan="8" className="dash-table-td">
                      <div className="dash-skeleton" style={{ height: '20px', width: '100%' }} />
                    </td>
                  </tr>
                ))
              ) : sections.length === 0 ? (
                <tr>
                  <td colSpan="8" className="dash-table-empty-notice">
                    No sections found. Run the Auto-Scheduler on the Master Schedule page to generate them.
                  </td>
                </tr>
              ) : sections.map(s => (
                <motion.tr key={s.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="dash-table-tr">
                  <td className="dash-table-td">
                    <div className="dash-layout-vertical">
                      <span className="dash-heading-item">{s.courseCode}</span>
                      <span className="dash-label-subtle">{s.courseTitle}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <div className="dash-badge-instructor">
                        {s.instructorName?.split(' ').map(n => n[0]).join('')}
                      </div>
                      <span className="dash-label-primary">{s.instructorName}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-primary">{s.daysOfWeek} {s.startTime}–{s.endTime}</span>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-primary">{s.classroomLabel}</span>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-code-badge">{s.semester}</span>
                  </td>
                  <td className="dash-table-td">
                    {s.isPublished ? (
                      <div className="dash-status-scheduled">
                        <CheckCircle2 style={{ width: '14px', height: '14px' }} />
                        <span>Live</span>
                      </div>
                    ) : (
                      <span className="dash-label-subtle">Draft</span>
                    )}
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <Users style={{ width: '14px', height: '14px', color: 'var(--color-slate-500)' }} />
                      <span className="dash-label-data">{s.enrolledCount}/{s.capacity}</span>
                    </div>
                  </td>
                  <td className="dash-table-td dash-table-align-end">
                    <button
                      onClick={() => handleDelete(s.id)}
                      className="dash-action-button"
                      title="Delete section"
                      style={{ color: 'var(--color-red-400)' }}
                    >
                      <Trash2 style={{ width: '14px', height: '14px' }} />
                    </button>
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

// ─── INSTRUCTOR VIEW ─────────────────────────────────────────────────────────
const InstructorSectionsView = ({ user }) => {
  const [sections, setSections] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchMySections = async () => {
      try {
        setIsLoading(true);
        const data = await sectionService.getAll({ instructorId: user.id });
        setSections(data);
      } catch {
        toast.error('Failed to load your sections');
      } finally {
        setIsLoading(false);
      }
    };
    fetchMySections();
  }, [user.id]);

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">My Assigned Classes</h1>
          <p className="dash-subtitle">Sections you are currently teaching.</p>
        </div>
      </header>

      <div className="dash-sections-grid">
        {isLoading ? (
          [1, 2].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: '200px' }} />)
        ) : sections.length === 0 ? (
          <div className="dash-card" style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '4rem' }}>
            <GraduationCap style={{ margin: '0 auto 1rem', width: '48px', height: '48px', color: 'var(--color-slate-600)' }} />
            <p className="dash-label-primary">No sections assigned yet.</p>
            <p className="dash-label-subtle">Update your availability so the scheduler can assign you classes.</p>
          </div>
        ) : sections.map(s => (
          <motion.div
            key={s.id}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="dash-card-section-item"
          >
            <div className="dash-card-flex-header">
              <div>
                <h3 className="dash-section-code">{s.courseCode}</h3>
                <p className="dash-section-course-title">{s.courseTitle}</p>
              </div>
              <span className="dash-code-badge">{s.semester}</span>
            </div>

            <div className="dash-card-info-grid" style={{ marginTop: '1rem' }}>
              <div className="dash-info-item">
                <Clock className="dash-info-icon" />
                <span className="dash-label-primary">{s.daysOfWeek} · {s.startTime} – {s.endTime}</span>
              </div>
              <div className="dash-info-item">
                <Building className="dash-info-icon" />
                <span className="dash-label-primary">{s.classroomLabel}</span>
              </div>
              <div className="dash-info-item">
                <Users className="dash-info-icon" />
                <span className="dash-label-primary">{s.enrolledCount} / {s.capacity} students enrolled</span>
              </div>
            </div>
          </motion.div>
        ))}
      </div>
    </div>
  );
};

// ─── STUDENT VIEW ────────────────────────────────────────────────────────────
const StudentSectionsView = () => {
  const [sections, setSections] = useState([]);
  const [enrolledIds, setEnrolledIds] = useState(new Set());
  const [enrollments, setEnrollments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [sectionData, enrollmentData] = await Promise.all([
          sectionService.getAll(),
          enrollmentService.getMyEnrollments().catch(() => []),
        ]);
        setSections(sectionData);
        setEnrollments(enrollmentData);
        setEnrolledIds(new Set(
          enrollmentData.filter(e => e.status === 'Active').map(e => e.sectionId)
        ));
      } catch {
        toast.error('Failed to load sections');
      } finally {
        setIsLoading(false);
      }
    };
    fetchData();
  }, []);

  const handleEnroll = async (sectionId) => {
    setIsProcessing(sectionId);
    try {
      await enrollmentService.enroll(sectionId);
      setEnrolledIds(prev => new Set([...prev, sectionId]));
      toast.success('Successfully enrolled!');
    } catch (e) {
      toast.error(e.response?.data?.message || 'Enrollment failed. Check for prerequisite or schedule conflicts.');
    } finally {
      setIsProcessing(null);
    }
  };

  const handleDrop = async (sectionId) => {
    const enrollment = enrollments.find(e => e.sectionId === sectionId && e.status === 'Active');
    if (!enrollment) return;
    setIsProcessing(sectionId);
    try {
      await enrollmentService.drop(enrollment.id);
      setEnrolledIds(prev => { const s = new Set(prev); s.delete(sectionId); return s; });
      toast.success('Section dropped successfully.');
    } catch {
      toast.error('Failed to drop section.');
    } finally {
      setIsProcessing(null);
    }
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Enroll in Classes</h1>
          <p className="dash-subtitle">Browse available sections and manage your enrollment.</p>
        </div>
      </header>

      {sections.length === 0 && !isLoading && (
        <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
          <p className="dash-label-primary">No published sections available for enrollment yet.</p>
          <p className="dash-label-subtle">Check back after the Admin publishes the semester schedule.</p>
        </div>
      )}

      <div className="dash-sections-grid">
        {isLoading ? (
          [1, 2, 3].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: '220px' }} />)
        ) : sections.map(section => {
          const isEnrolled = enrolledIds.has(section.id);
          const isFull = section.enrolledCount >= section.capacity;
          return (
            <motion.div
              key={section.id}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className={`dash-card-section-item ${isEnrolled ? 'enrolled' : ''}`}
            >
              <div className="dash-card-flex-header">
                <div>
                  <h3 className="dash-section-code">{section.courseCode}</h3>
                  <p className="dash-section-course-title">{section.courseTitle}</p>
                </div>
                <span className="dash-code-badge">{section.semester}</span>
              </div>

              <div className="dash-card-info-grid" style={{ margin: '1rem 0' }}>
                <div className="dash-info-item">
                  <Clock className="dash-info-icon" />
                  <span className="dash-label-primary">{section.daysOfWeek} · {section.startTime} – {section.endTime}</span>
                </div>
                <div className="dash-info-item">
                  <User className="dash-info-icon" />
                  <span className="dash-label-primary">{section.instructorName}</span>
                </div>
                <div className="dash-info-item">
                  <Building className="dash-info-icon" />
                  <span className="dash-label-primary">{section.classroomLabel}</span>
                </div>
                <div className="dash-info-item">
                  <Users className="dash-info-icon" />
                  <span className="dash-label-primary">{section.enrolledCount}/{section.capacity} seats</span>
                </div>
              </div>

              {isEnrolled ? (
                <button
                  onClick={() => handleDrop(section.id)}
                  disabled={isProcessing === section.id}
                  className="dash-action-button"
                  style={{ width: '100%', justifyContent: 'center', color: 'var(--color-red-400)' }}
                >
                  <XCircle style={{ width: '16px', height: '16px' }} />
                  <span>{isProcessing === section.id ? 'Dropping...' : 'Drop Section'}</span>
                </button>
              ) : (
                <button
                  onClick={() => handleEnroll(section.id)}
                  disabled={isFull || isProcessing === section.id}
                  className="dash-primary-btn"
                  style={{ width: '100%' }}
                >
                  {isProcessing === section.id ? (
                    <span>Processing...</span>
                  ) : isFull ? (
                    <span>Section Full</span>
                  ) : (
                    <>
                      <CheckCircle2 style={{ width: '16px', height: '16px' }} />
                      <span>Enroll</span>
                    </>
                  )}
                </button>
              )}
            </motion.div>
          );
        })}
      </div>
    </div>
  );
};

export default Sections;
