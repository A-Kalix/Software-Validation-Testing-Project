import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { 
  Clock, User, Building, CheckCircle2, XCircle, 
  GraduationCap, Trash2, Plus, Pencil, Users
} from 'lucide-react';
import { sectionService } from '../services/sectionService';
import { enrollmentService } from '../services/enrollmentService';
import { toast } from 'sonner';
import { courseService } from '../services/courseService';
import { roomService } from '../services/roomService';
import { userService } from '../services/userService';

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
  
  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingSection, setEditingSection] = useState(null);
  
  // Form Data Lookups
  const [courses, setCourses] = useState([]);
  const [instructors, setInstructors] = useState([]);
  const [rooms, setRooms] = useState([]);

  // Form State
  const [formData, setFormData] = useState({
    courseId: '', instructorId: '', classroomId: '',
    semester: 'Fall 2026', daysOfWeek: 'MWF',
    startTime: '09:00:00', endTime: '09:50:00', capacity: 30
  });

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

  const fetchFormData = async () => {
    try {
      const [cData, rData, uData] = await Promise.all([
        courseService.getAll(),
        roomService.getAll(),
        userService.getAll()
      ]);
      setCourses(cData);
      setRooms(rData);
      setInstructors(uData.filter(u => u.role === 'Instructor'));
    } catch {
      toast.error('Failed to load form lookup data');
    }
  };

  useEffect(() => { 
    fetchSections(); 
    fetchFormData();
  }, []);

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

  const openAddModal = () => {
    setEditingSection(null);
    setFormData({
      courseId: courses[0]?.id || '', instructorId: instructors[0]?.id || '', classroomId: rooms[0]?.id || '',
      semester: 'Fall 2026', daysOfWeek: 'MWF', startTime: '09:00', endTime: '09:50', capacity: 30
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsSubmitting(true);
    try {
      const payload = {
        ...formData,
        startTime: formData.startTime.length === 5 ? `${formData.startTime}:00` : formData.startTime,
        endTime: formData.endTime.length === 5 ? `${formData.endTime}:00` : formData.endTime,
      };
      
      if (editingSection) {
        await sectionService.update(editingSection.id, payload);
        toast.success('Section updated');
      } else {
        await sectionService.create(payload);
        toast.success('Section created');
      }
      fetchSections();
      setIsModalOpen(false);
    } catch (error) {
      toast.error(error.response?.data?.message || 'Failed to save section');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Manage Sections</h1>
          <p className="dash-subtitle">All course sections — create, edit, and delete.</p>
        </div>
        <button onClick={openAddModal} className="dash-primary-btn">
          <Plus className="dash-icon-primary" /> Add Section
        </button>
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
                    <button onClick={() => handleDelete(s.id)} className="dash-action-button" style={{ color: 'var(--color-red-400)' }}>
                      <Trash2 style={{ width: '14px', height: '14px' }} />
                    </button>
                  </td>
                </motion.tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {isModalOpen && (
        <div className="dash-modal-overlay">
          <motion.div initial={{ scale: 0.95, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} className="dash-modal-card" style={{ maxWidth: '600px' }}>
            <div className="dash-modal-header">
              <h2 className="dash-modal-title">{editingSection ? 'Edit Section' : 'Create Section'}</h2>
              <button onClick={() => setIsModalOpen(false)} className="dash-modal-close"><XCircle /></button>
            </div>
            
            <form onSubmit={handleSubmit} className="dash-form-body">
              <div className="dash-form-grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
                <div className="dash-input-group">
                  <label className="dash-input-label">Course</label>
                  <select className="dash-input" value={formData.courseId} onChange={e => setFormData({...formData, courseId: e.target.value})} required>
                    <option value="">Select Course...</option>
                    {courses.map(c => <option key={c.id} value={c.id}>{c.courseCode} - {c.title}</option>)}
                  </select>
                </div>
                
                <div className="dash-input-group">
                  <label className="dash-input-label">Instructor</label>
                  <select className="dash-input" value={formData.instructorId} onChange={e => setFormData({...formData, instructorId: e.target.value})} required>
                    <option value="">Select Instructor...</option>
                    {instructors.map(u => <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>)}
                  </select>
                </div>
                
                <div className="dash-input-group">
                  <label className="dash-input-label">Room</label>
                  <select className="dash-input" value={formData.classroomId} onChange={e => setFormData({...formData, classroomId: e.target.value})} required>
                    <option value="">Select Room...</option>
                    {rooms.map(r => <option key={r.id} value={r.id}>{r.building} {r.roomNumber}</option>)}
                  </select>
                </div>

                <div className="dash-input-group">
                  <label className="dash-input-label">Semester</label>
                  <input type="text" className="dash-input" value={formData.semester} onChange={e => setFormData({...formData, semester: e.target.value})} required />
                </div>

                <div className="dash-input-group">
                  <label className="dash-input-label">Days</label>
                  <input type="text" placeholder="MWF" className="dash-input" value={formData.daysOfWeek} onChange={e => setFormData({...formData, daysOfWeek: e.target.value})} required />
                </div>

                <div className="dash-input-group">
                  <label className="dash-input-label">Capacity</label>
                  <input type="number" min="1" max="1000" className="dash-input" value={formData.capacity} onChange={e => setFormData({...formData, capacity: parseInt(e.target.value)})} required />
                </div>

                <div className="dash-input-group">
                  <label className="dash-input-label">Start Time</label>
                  <input type="time" className="dash-input" value={formData.startTime} onChange={e => setFormData({...formData, startTime: e.target.value})} required />
                </div>

                <div className="dash-input-group">
                  <label className="dash-input-label">End Time</label>
                  <input type="time" className="dash-input" value={formData.endTime} onChange={e => setFormData({...formData, endTime: e.target.value})} required />
                </div>
              </div>
              
              <div className="dash-modal-actions">
                <button type="button" onClick={() => setIsModalOpen(false)} className="dash-secondary-btn">Cancel</button>
                <button type="submit" disabled={isSubmitting} className="dash-primary-btn">
                  {isSubmitting ? 'Saving...' : 'Save Section'}
                </button>
              </div>
            </form>
          </motion.div>
        </div>
      )}
    </div>
  );
};

// ─── INSTRUCTOR VIEW ─────────────────────────────────────────────────────────
const BLOCK_COLORS = [
  { bg: 'rgba(99,102,241,0.18)',  border: '#6366f1', text: '#a5b4fc' },
  { bg: 'rgba(14,165,233,0.18)',  border: '#0ea5e9', text: '#7dd3fc' },
  { bg: 'rgba(168,85,247,0.18)',  border: '#a855f7', text: '#c4b5fd' },
  { bg: 'rgba(236,72,153,0.18)',  border: '#ec4899', text: '#f9a8d4' },
  { bg: 'rgba(34,197,94,0.18)',   border: '#22c55e', text: '#86efac' },
  { bg: 'rgba(245,158,11,0.18)',  border: '#f59e0b', text: '#fcd34d' },
  { bg: 'rgba(239,68,68,0.18)',   border: '#ef4444', text: '#fca5a5' },
  { bg: 'rgba(20,184,166,0.18)',  border: '#14b8a6', text: '#5eead4' },
];
const hashColor = (str) => {
  let h = 0;
  for (let i = 0; i < (str || '').length; i++) h = ((h << 5) - h + str.charCodeAt(i)) | 0;
  return BLOCK_COLORS[Math.abs(h) % BLOCK_COLORS.length];
};
const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'];
const HOURS = Array.from({ length: 11 }, (_, i) => i + 8);
const matchesDay = (daysStr, dayLabel) => {
  if (!daysStr) return false;
  const d = daysStr.trim();
  if (d === dayLabel || d.includes(dayLabel)) return true;
  const map = { Mon: /M(?!o)/, Tue: /T(?!h)/, Wed: /W/, Thu: /Th/, Fri: /F/ };
  return map[dayLabel]?.test(d) ?? false;
};

const InstructorSectionsView = ({ user }) => {
  const [sections, setSections] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [viewMode, setViewMode] = useState('grid');

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

  const getBlock = (day, hour) =>
    sections.find(s => matchesDay(s.daysOfWeek, day) && parseInt(String(s.startTime).split(':')[0]) === hour);

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">My Teaching Schedule</h1>
          <p className="dash-subtitle">{sections.length} section{sections.length !== 1 ? 's' : ''} assigned this semester.</p>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => setViewMode('grid')}
            className={viewMode === 'grid' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}>
            <Clock style={{ width: 14, height: 14 }} /><span>Timetable</span>
          </button>
          <button onClick={() => setViewMode('list')}
            className={viewMode === 'list' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}>
            <Users style={{ width: 14, height: 14 }} /><span>Card View</span>
          </button>
        </div>
      </header>

      {isLoading ? (
        <div className="dash-card dash-skeleton" style={{ height: '450px' }} />
      ) : sections.length === 0 ? (
        <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
          <GraduationCap style={{ margin: '0 auto 1rem', width: 48, height: 48, color: '#475569' }} />
          <p className="dash-label-primary">No sections assigned yet.</p>
          <p className="dash-label-subtle">Update your availability so the scheduler can assign you classes.</p>
        </div>
      ) : viewMode === 'grid' ? (
        /* ──── TIMETABLE GRID ──── */
        <div className="dash-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '700px' }}>
              <thead>
                <tr>
                  <th style={{ width: 70, padding: '0.75rem 0.5rem', textAlign: 'center', color: '#64748b', fontSize: '0.75rem', fontWeight: 600, textTransform: 'uppercase', borderBottom: '1px solid #1e293b', background: '#0f172a' }}>Time</th>
                  {DAYS.map(d => (
                    <th key={d} style={{ padding: '0.75rem', textAlign: 'center', color: '#e2e8f0', fontSize: '0.85rem', fontWeight: 600, borderBottom: '1px solid #1e293b', background: '#0f172a', letterSpacing: '0.05em' }}>{d}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {HOURS.map(hour => (
                  <tr key={hour}>
                    <td style={{ padding: '0.25rem 0.5rem', textAlign: 'center', verticalAlign: 'top', color: '#64748b', fontSize: '0.75rem', fontWeight: 500, borderRight: '1px solid #1e293b', borderBottom: '1px solid rgba(30,41,59,0.5)', background: '#0f172a', width: 70, height: 64 }}>
                      {`${String(hour).padStart(2, '0')}:00`}
                    </td>
                    {DAYS.map(day => {
                      const block = getBlock(day, hour);
                      const color = block ? hashColor(block.courseCode) : null;
                      return (
                        <td key={`${day}-${hour}`} style={{ padding: 2, verticalAlign: 'top', borderBottom: '1px solid rgba(30,41,59,0.5)', borderRight: '1px solid rgba(30,41,59,0.3)', height: 64, position: 'relative' }}>
                          {block && (
                            <motion.div initial={{ opacity: 0, scale: 0.92 }} animate={{ opacity: 1, scale: 1 }}
                              style={{ background: color.bg, border: `1px solid ${color.border}`, borderLeft: `3px solid ${color.border}`, borderRadius: 6, padding: '0.35rem 0.5rem', height: '100%', display: 'flex', flexDirection: 'column', gap: 2 }}>
                              <span style={{ color: color.text, fontWeight: 700, fontSize: '0.78rem', lineHeight: 1.2 }}>{block.courseCode}</span>
                              <span style={{ color: '#94a3b8', fontSize: '0.65rem', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{block.courseTitle}</span>
                              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 'auto', color: '#64748b', fontSize: '0.6rem' }}>
                                <span>{block.classroomLabel}</span>
                                <span>{block.enrolledCount}/{block.capacity}</span>
                              </div>
                            </motion.div>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* Legend */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', padding: '0.75rem 1rem', borderTop: '1px solid #1e293b', background: '#0f172a' }}>
            {sections.map(s => {
              const c = hashColor(s.courseCode);
              return (
                <div key={s.id} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                  <div style={{ width: 10, height: 10, borderRadius: 2, background: c.border }} />
                  <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>{s.courseCode} — {s.enrolledCount} students</span>
                </div>
              );
            })}
          </div>
        </div>
      ) : (
        /* ──── CARD VIEW ──── */
        <div className="dash-sections-grid">
          {sections.map(s => {
            const color = hashColor(s.courseCode);
            return (
              <motion.div key={s.id} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
                className="dash-card-section-item" style={{ borderLeft: `3px solid ${color.border}` }}>
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
                    <span className="dash-label-primary">{s.daysOfWeek} · {String(s.startTime).substring(0,5)} – {String(s.endTime).substring(0,5)}</span>
                  </div>
                  <div className="dash-info-item">
                    <Building className="dash-info-icon" />
                    <span className="dash-label-primary">{s.classroomLabel}</span>
                  </div>
                  <div className="dash-info-item">
                    <Users className="dash-info-icon" />
                    <span className="dash-label-primary">{s.enrolledCount} / {s.capacity} students</span>
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
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
  const [tab, setTab] = useState('available'); // 'available' | 'enrolled'

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const [sectionData, enrollmentData] = await Promise.all([
        sectionService.getAll(),
        enrollmentService.getMyEnrollments().catch(() => []),
      ]);
      // Students only see PUBLISHED sections
      setSections(sectionData.filter(s => s.isPublished === true));
      setEnrollments(enrollmentData);
      // Status from backend is a string e.g. 'Active'
      setEnrolledIds(new Set(
        (enrollmentData || []).filter(e => e.status?.toLowerCase() === 'active').map(e => e.sectionId)
      ));
    } catch (err) {
      console.error('fetchData error:', err);
      toast.error('Failed to load sections');
    } finally {
      setIsLoading(false);
    }
  };

  const handleEnroll = async (sectionId) => {
    setIsProcessing(sectionId);
    try {
      await enrollmentService.enroll(sectionId);
      setEnrolledIds(prev => new Set([...prev, sectionId]));
      toast.success('Successfully enrolled!');
      await fetchData(); // Refresh counts
    } catch (e) {
      toast.error(e.response?.data?.message || 'Enrollment failed. Check for prerequisite or schedule conflicts.');
    } finally {
      setIsProcessing(null);
    }
  };

  const handleDrop = async (sectionId) => {
    const enrollment = enrollments.find(e => e.sectionId === sectionId && e.status?.toLowerCase() === 'active');
    if (!enrollment) return;
    setIsProcessing(sectionId);
    try {
      await enrollmentService.drop(enrollment.id);
      setEnrolledIds(prev => { const s = new Set(prev); s.delete(sectionId); return s; });
      toast.success('Section dropped successfully.');
      await fetchData();
    } catch {
      toast.error('Failed to drop section.');
    } finally {
      setIsProcessing(null);
    }
  };

  const activeEnrollments = (enrollments || []).filter(e => e.status?.toLowerCase() === 'active');
  const availableSections = sections.filter(s => !enrolledIds.has(s.id));

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">My Courses</h1>
          <p className="dash-subtitle">Browse available sections and manage your enrollment.</p>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            onClick={() => setTab('available')}
            className={tab === 'available' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 1rem', fontSize: '0.85rem' }}
          >
            Available ({availableSections.length})
          </button>
          <button
            onClick={() => setTab('enrolled')}
            className={tab === 'enrolled' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 1rem', fontSize: '0.85rem' }}
          >
            Enrolled ({activeEnrollments.length})
          </button>
        </div>
      </header>

      {/* AVAILABLE TAB */}
      {tab === 'available' && (
        <>
          {!isLoading && availableSections.length === 0 && (
            <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
              <p className="dash-label-primary">No published sections available for enrollment yet.</p>
              <p className="dash-label-subtle">Check back after the Admin publishes the semester schedule.</p>
            </div>
          )}
          <div className="dash-sections-grid">
            {isLoading ? (
              [1, 2, 3].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: '220px' }} />)
            ) : availableSections.map(section => {
              const isFull = section.enrolledCount >= section.capacity;
              return (
                <motion.div
                  key={section.id}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="dash-card-section-item"
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
                      <span className="dash-label-primary">{section.daysOfWeek} · {String(section.startTime).substring(0,5)} – {String(section.endTime).substring(0,5)}</span>
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
                  <button
                    onClick={() => handleEnroll(section.id)}
                    disabled={isFull || isProcessing === section.id}
                    className="dash-primary-btn"
                    style={{ width: '100%' }}
                  >
                    {isProcessing === section.id ? <span>Processing...</span>
                      : isFull ? <span>Section Full</span>
                      : <><CheckCircle2 style={{ width: '16px', height: '16px' }} /><span>Enroll</span></>}
                  </button>
                </motion.div>
              );
            })}
          </div>
        </>
      )}

      {/* ENROLLED TAB */}
      {tab === 'enrolled' && (
        <>
          {!isLoading && activeEnrollments.length === 0 && (
            <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
              <p className="dash-label-primary">You haven't enrolled in any courses yet.</p>
              <button onClick={() => setTab('available')} className="dash-primary-btn" style={{ marginTop: '1rem' }}>
                Browse Available Courses
              </button>
            </div>
          )}
          <div className="dash-sections-grid">
            {isLoading ? (
              [1, 2].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: '220px' }} />)
            ) : activeEnrollments.map(enrollment => (
              <motion.div
                key={enrollment.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="dash-card-section-item enrolled"
              >
                <div className="dash-card-flex-header">
                  <div>
                    <h3 className="dash-section-code">{enrollment.courseCode}</h3>
                    <p className="dash-section-course-title">{enrollment.courseTitle}</p>
                  </div>
                  <span className="dash-status-scheduled" style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '999px' }}>Enrolled</span>
                </div>
                <div className="dash-card-info-grid" style={{ margin: '1rem 0' }}>
                  <div className="dash-info-item">
                    <Clock className="dash-info-icon" />
                    <span className="dash-label-primary">{enrollment.daysOfWeek} · {enrollment.startTime} – {enrollment.endTime}</span>
                  </div>
                  <div className="dash-info-item">
                    <Building className="dash-info-icon" />
                    <span className="dash-label-primary">{enrollment.classroomName}</span>
                  </div>
                  <div className="dash-info-item">
                    <GraduationCap className="dash-info-icon" />
                    <span className="dash-label-primary">{enrollment.semester}</span>
                  </div>
                </div>
                <button
                  onClick={() => handleDrop(enrollment.sectionId)}
                  disabled={isProcessing === enrollment.sectionId}
                  className="dash-action-button"
                  style={{ width: '100%', justifyContent: 'center', color: '#f87171' }}
                >
                  <XCircle style={{ width: '16px', height: '16px' }} />
                  <span>{isProcessing === enrollment.sectionId ? 'Dropping...' : 'Drop Section'}</span>
                </button>
              </motion.div>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

export default Sections;

