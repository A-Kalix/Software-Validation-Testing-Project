import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { CalendarDays, Trash2, Clock, MapPin, GraduationCap, LayoutGrid, List, User } from 'lucide-react';
import { enrollmentService } from '../services/enrollmentService';
import { toast } from 'sonner';

// Color palette for course blocks (deterministic per courseCode)
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
const HOURS = Array.from({ length: 11 }, (_, i) => i + 8); // 08:00 – 18:00

// Parse daysOfWeek string from scheduler (e.g. "Mon", "MWF", "Tue", "Thu")
const matchesDay = (daysStr, dayLabel) => {
  if (!daysStr) return false;
  const d = daysStr.trim();
  // Exact match: "Mon", "Tue", "Wed", "Thu", "Fri"
  if (d === dayLabel) return true;
  // 3-letter abbreviation inside longer string
  if (d.includes(dayLabel)) return true;
  // Single-letter codes: M=Mon W=Wed F=Fri, T/Th handled specially
  const map = { Mon: /M(?!o)/, Tue: /T(?!h)/, Wed: /W/, Thu: /Th/, Fri: /F/ };
  return map[dayLabel]?.test(d) ?? false;
};

const MySchedule = () => {
  const [enrollments, setEnrollments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isDropping, setIsDropping] = useState(null);
  const [viewMode, setViewMode] = useState('grid');

  useEffect(() => { fetchSchedule(); }, []);

  const fetchSchedule = async () => {
    try {
      const data = await enrollmentService.getMyEnrollments();
      setEnrollments((data || []).filter(e => e.status?.toLowerCase() === 'active'));
    } catch (error) {
      console.error('Error fetching schedule:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDrop = async (enrollmentId) => {
    setIsDropping(enrollmentId);
    try {
      await enrollmentService.drop(enrollmentId);
      setEnrollments(prev => prev.filter(e => e.id !== enrollmentId));
      toast.success('Course dropped');
    } catch {
      toast.error('Could not drop course');
    } finally {
      setIsDropping(null);
    }
  };

  // Build lookup: for each (day, hour) → enrollment or null
  const getBlock = (day, hour) => {
    return enrollments.find(e => {
      if (!matchesDay(e.daysOfWeek, day)) return false;
      const startH = parseInt(String(e.startTime).split(':')[0]);
      return startH === hour;
    });
  };

  // Calculate if a slot is a continuation of a multi-hour block
  const isContinuation = (day, hour) => {
    return enrollments.some(e => {
      if (!matchesDay(e.daysOfWeek, day)) return false;
      const startH = parseInt(String(e.startTime).split(':')[0]);
      const endH = parseInt(String(e.endTime).split(':')[0]);
      return hour > startH && hour < endH;
    });
  };

  const totalCredits = enrollments.reduce((sum, e) => sum + (e.credits || 3), 0);

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Academic Timetable</h1>
          <p className="dash-subtitle">
            {enrollments.length} course{enrollments.length !== 1 ? 's' : ''} · {totalCredits} credit hours
          </p>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => setViewMode('grid')}
            className={viewMode === 'grid' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}>
            <LayoutGrid style={{ width: 14, height: 14 }} /><span>Grid</span>
          </button>
          <button onClick={() => setViewMode('list')}
            className={viewMode === 'list' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}>
            <List style={{ width: 14, height: 14 }} /><span>List</span>
          </button>
        </div>
      </header>

      {isLoading ? (
        <div className="dash-card dash-skeleton" style={{ height: '500px' }} />
      ) : enrollments.length === 0 ? (
        <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
          <CalendarDays style={{ width: 56, height: 56, color: '#334155', margin: '0 auto 1rem' }} />
          <p className="dash-label-primary" style={{ fontSize: '1.05rem' }}>Your schedule is empty</p>
          <p className="dash-label-subtle" style={{ marginBottom: '1.5rem' }}>Enroll in courses to see them here.</p>
          <button onClick={() => window.location.href = '/dashboard/sections'} className="dash-primary-btn">
            Browse Courses
          </button>
        </div>
      ) : viewMode === 'grid' ? (
        /* ──────── TIMETABLE GRID ──────── */
        <div className="dash-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '700px' }}>
              <thead>
                <tr>
                  <th style={{
                    width: '70px', padding: '0.75rem 0.5rem', textAlign: 'center',
                    color: '#64748b', fontSize: '0.75rem', fontWeight: 600, textTransform: 'uppercase',
                    borderBottom: '1px solid #1e293b', background: '#0f172a'
                  }}>Time</th>
                  {DAYS.map(d => (
                    <th key={d} style={{
                      padding: '0.75rem', textAlign: 'center',
                      color: '#e2e8f0', fontSize: '0.85rem', fontWeight: 600,
                      borderBottom: '1px solid #1e293b', background: '#0f172a',
                      letterSpacing: '0.05em'
                    }}>{d}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {HOURS.map(hour => {
                  const timeLabel = `${String(hour).padStart(2, '0')}:00`;
                  return (
                    <tr key={hour}>
                      <td style={{
                        padding: '0.25rem 0.5rem', textAlign: 'center', verticalAlign: 'top',
                        color: '#64748b', fontSize: '0.75rem', fontWeight: 500,
                        borderRight: '1px solid #1e293b', borderBottom: '1px solid rgba(30,41,59,0.5)',
                        background: '#0f172a', width: '70px', minHeight: '60px'
                      }}>{timeLabel}</td>
                      {DAYS.map(day => {
                        const block = getBlock(day, hour);
                        const cont = !block && isContinuation(day, hour);
                        const color = block ? hashColor(block.courseCode) : null;

                        return (
                          <td key={`${day}-${hour}`} style={{
                            padding: '2px', verticalAlign: 'top',
                            borderBottom: '1px solid rgba(30,41,59,0.5)',
                            borderRight: '1px solid rgba(30,41,59,0.3)',
                            height: '60px', position: 'relative',
                            background: cont ? 'rgba(99,102,241,0.04)' : 'transparent'
                          }}>
                            {block && (
                              <motion.div
                                initial={{ opacity: 0, scale: 0.92 }}
                                animate={{ opacity: 1, scale: 1 }}
                                style={{
                                  background: color.bg,
                                  border: `1px solid ${color.border}`,
                                  borderLeft: `3px solid ${color.border}`,
                                  borderRadius: '6px',
                                  padding: '0.4rem 0.5rem',
                                  height: '100%',
                                  display: 'flex', flexDirection: 'column',
                                  gap: '2px', cursor: 'default',
                                  transition: 'transform 0.15s',
                                }}>
                                <span style={{ color: color.text, fontWeight: 700, fontSize: '0.78rem', lineHeight: 1.2 }}>
                                  {block.courseCode}
                                </span>
                                <span style={{ color: '#94a3b8', fontSize: '0.68rem', lineHeight: 1.2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                  {block.courseTitle}
                                </span>
                                <span style={{ color: '#64748b', fontSize: '0.62rem', display: 'flex', alignItems: 'center', gap: '3px', marginTop: 'auto' }}>
                                  <MapPin style={{ width: 9, height: 9 }} />
                                  {block.classroomName}
                                </span>
                              </motion.div>
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Legend */}
          <div style={{
            display: 'flex', flexWrap: 'wrap', gap: '0.75rem',
            padding: '0.75rem 1rem', borderTop: '1px solid #1e293b',
            background: '#0f172a'
          }}>
            {enrollments.map(e => {
              const c = hashColor(e.courseCode);
              return (
                <div key={e.id} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                  <div style={{ width: 10, height: 10, borderRadius: 2, background: c.border }} />
                  <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>{e.courseCode}</span>
                </div>
              );
            })}
          </div>
        </div>
      ) : (
        /* ──────── LIST VIEW ──────── */
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          <AnimatePresence>
            {enrollments.map((e) => {
              const color = hashColor(e.courseCode);
              return (
                <motion.div key={e.id}
                  initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, x: 20 }}
                  className="dash-card" style={{
                    display: 'flex', alignItems: 'center', gap: '1rem',
                    padding: '1rem 1.25rem', borderLeft: `3px solid ${color.border}`
                  }}>
                  {/* Color dot + Course Info */}
                  <div style={{ flex: 1 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.3rem' }}>
                      <span style={{ color: color.text, fontWeight: 700, fontSize: '0.95rem' }}>{e.courseCode}</span>
                      <span style={{ color: '#475569', fontSize: '0.8rem' }}>·</span>
                      <span style={{ color: '#94a3b8', fontSize: '0.85rem' }}>{e.courseTitle}</span>
                    </div>
                    <div style={{ display: 'flex', gap: '1.25rem', flexWrap: 'wrap' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', color: '#64748b', fontSize: '0.8rem' }}>
                        <Clock style={{ width: 13, height: 13 }} />
                        {e.daysOfWeek} · {e.startTime} – {e.endTime}
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', color: '#64748b', fontSize: '0.8rem' }}>
                        <MapPin style={{ width: 13, height: 13 }} />
                        {e.classroomName}
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', color: '#64748b', fontSize: '0.8rem' }}>
                        <GraduationCap style={{ width: 13, height: 13 }} />
                        {e.semester}
                      </span>
                    </div>
                  </div>

                  {/* Drop button */}
                  <button onClick={() => handleDrop(e.id)} disabled={isDropping === e.id}
                    style={{
                      background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.2)',
                      borderRadius: '8px', padding: '0.4rem 0.75rem', cursor: 'pointer',
                      color: '#f87171', fontSize: '0.8rem', fontWeight: 500,
                      display: 'flex', alignItems: 'center', gap: '0.3rem',
                      transition: 'all 0.15s', flexShrink: 0,
                      opacity: isDropping === e.id ? 0.5 : 1
                    }}>
                    <Trash2 style={{ width: 13, height: 13 }} />
                    {isDropping === e.id ? 'Dropping...' : 'Drop'}
                  </button>
                </motion.div>
              );
            })}
          </AnimatePresence>
        </div>
      )}
    </div>
  );
};

export default MySchedule;
