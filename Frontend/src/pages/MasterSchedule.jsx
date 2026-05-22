import React, { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Play, Search, AlertTriangle, CheckCircle2, Clock, Globe, Lock, ShieldAlert, X, Wrench, ArrowRight, LayoutGrid, List, MapPin } from 'lucide-react';
import { sectionService } from '../services/sectionService';
import client from '../api/client';
import { toast } from 'sonner';

// Timetable helpers
const TT_COLORS = [
  { bg: 'rgba(99,102,241,0.18)', border: '#6366f1', text: '#a5b4fc' },
  { bg: 'rgba(14,165,233,0.18)', border: '#0ea5e9', text: '#7dd3fc' },
  { bg: 'rgba(168,85,247,0.18)', border: '#a855f7', text: '#c4b5fd' },
  { bg: 'rgba(236,72,153,0.18)', border: '#ec4899', text: '#f9a8d4' },
  { bg: 'rgba(34,197,94,0.18)',  border: '#22c55e', text: '#86efac' },
  { bg: 'rgba(245,158,11,0.18)', border: '#f59e0b', text: '#fcd34d' },
  { bg: 'rgba(239,68,68,0.18)',  border: '#ef4444', text: '#fca5a5' },
  { bg: 'rgba(20,184,166,0.18)', border: '#14b8a6', text: '#5eead4' },
];
const ttHash = (s) => { let h=0; for(let i=0;i<(s||'').length;i++) h=((h<<5)-h+s.charCodeAt(i))|0; return TT_COLORS[Math.abs(h)%TT_COLORS.length]; };
const TT_DAYS = ['Mon','Tue','Wed','Thu','Fri'];
const TT_HOURS = Array.from({length:11},(_,i)=>i+8);
const ttMatchDay = (d,label) => { if(!d) return false; d=d.trim(); if(d===label||d.includes(label)) return true; const m={Mon:/M(?!o)/,Tue:/T(?!h)/,Wed:/W/,Thu:/Th/,Fri:/F/}; return m[label]?.test(d)??false; };

// ── Custom Confirm Modal (replaces window.confirm) ────────────────────────────
const ConfirmModal = ({ isOpen, title, message, onConfirm, onCancel, confirmLabel = 'Confirm', danger = false }) => {
  if (!isOpen) return null;
  return (
    <div className="dash-modal-overlay" style={{ zIndex: 9999 }}>
      <motion.div initial={{ scale: 0.95, opacity: 0 }} animate={{ scale: 1, opacity: 1 }}
        style={{ background: '#1e293b', border: '1px solid #334155', borderRadius: '12px', padding: '1.5rem', maxWidth: '420px', width: '90%' }}>
        <h3 style={{ color: '#f1f5f9', fontWeight: 700, marginBottom: '0.5rem', fontSize: '1.05rem' }}>{title}</h3>
        <p style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '1.5rem', lineHeight: 1.5 }}>{message}</p>
        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
          <button onClick={onCancel} className="dash-secondary-btn" style={{ padding: '0.45rem 1rem' }}>Cancel</button>
          <button onClick={onConfirm} className="dash-primary-btn"
            style={{ padding: '0.45rem 1rem', background: danger ? '#dc2626' : undefined }}>
            {confirmLabel}
          </button>
        </div>
      </motion.div>
    </div>
  );
};

// ── Conflict Resolution Engine ────────────────────────────────────────────────
const toMin = t => { const p = String(t).split(':'); return parseInt(p[0]) * 60 + parseInt(p[1]); };
const timesOverlap = (s1, e1, s2, e2) => toMin(s1) < toMin(e2) && toMin(s2) < toMin(e1);
const sharesDay = (a, b) => (a || '').toUpperCase().split('').some(d => (b || '').toUpperCase().includes(d));

function detectConflicts(sections) {
  const found = [], ids = new Set();
  for (let i = 0; i < sections.length; i++) {
    for (let j = i + 1; j < sections.length; j++) {
      const a = sections[i], b = sections[j];
      if (!sharesDay(a.daysOfWeek, b.daysOfWeek)) continue;
      if (!timesOverlap(a.startTime, a.endTime, b.startTime, b.endTime)) continue;
      if (a.instructorId && a.instructorId === b.instructorId) {
        found.push({ type: 'Instructor', severity: 'critical', affectedId: b.id, keepId: a.id,
          message: `${a.instructorName} double-booked`, detail: `${a.courseCode} vs ${b.courseCode} on ${a.daysOfWeek} at ${String(a.startTime).substring(0,5)}`,
          sectionA: a, sectionB: b });
        ids.add(a.id); ids.add(b.id);
      }
      if (a.classroomId && a.classroomId === b.classroomId) {
        found.push({ type: 'Room', severity: 'critical', affectedId: b.id, keepId: a.id,
          message: `${a.classroomLabel} double-booked`, detail: `${a.courseCode} vs ${b.courseCode} on ${a.daysOfWeek} at ${String(a.startTime).substring(0,5)}`,
          sectionA: a, sectionB: b });
        ids.add(a.id); ids.add(b.id);
      }
    }
  }
  // Capacity check — enrollment exceeds or matches room capacity
  for (const s of sections) {
    if (s.enrolledCount != null && s.capacity != null && s.enrolledCount >= s.capacity) {
      found.push({ type: 'Capacity', severity: s.enrolledCount > s.capacity ? 'critical' : 'warning',
        affectedId: s.id, keepId: s.id,
        message: `${s.classroomLabel || 'Room'} at capacity for ${s.courseCode}`,
        detail: `${s.enrolledCount}/${s.capacity} seats filled — no room for more students`,
        sectionA: s, sectionB: s });
      ids.add(s.id);
    }
  }
  return { found, ids };
}

function generateResolutions(conflicts, sections, rooms) {
  return conflicts.map((c, i) => {
    const { type, sectionA, sectionB } = c;
    const affected = sectionB; // we'll move B to fix the conflict

    if (type === 'Room') {
      // Find another room free at that time/day
      const occupiedRoomIds = new Set(
        sections.filter(s => s.id !== affected.id && sharesDay(s.daysOfWeek, affected.daysOfWeek) &&
          timesOverlap(s.startTime, s.endTime, affected.startTime, affected.endTime))
          .map(s => s.classroomId)
      );
      const altRoom = (rooms || []).find(r => !occupiedRoomIds.has(r.id) && r.id !== affected.classroomId);
      if (altRoom) {
        return { conflictIndex: i, sectionId: affected.id, fix: 'change_room',
          label: `Move ${affected.courseCode} to ${altRoom.building} ${altRoom.roomNumber}`,
          payload: { classroomId: altRoom.id },
          description: `Reassign ${affected.courseCode} from ${affected.classroomLabel} → ${altRoom.building} ${altRoom.roomNumber}` };
      }
    }

    if (type === 'Instructor') {
      // Suggest a different day string to avoid overlap (shift to next day pattern)
      const dayShifts = { 'Mon': 'Wed', 'Tue': 'Thu', 'Wed': 'Fri', 'Thu': 'Mon', 'Fri': 'Tue' };
      const currentDay = affected.daysOfWeek?.substring(0, 3);
      const newDay = dayShifts[currentDay] || 'Mon';
      // Check newDay is free for this instructor
      const instructorBusy = sections.filter(s =>
        s.id !== affected.id && s.instructorId === affected.instructorId &&
        sharesDay(s.daysOfWeek, newDay) &&
        timesOverlap(s.startTime, s.endTime, affected.startTime, affected.endTime)
      );
      if (instructorBusy.length === 0) {
        return { conflictIndex: i, sectionId: affected.id, fix: 'change_day',
          label: `Move ${affected.courseCode} to ${newDay}`,
          payload: { daysOfWeek: newDay },
          description: `Reschedule ${affected.courseCode} from ${affected.daysOfWeek} → ${newDay} (same time)` };
      }
    }

    if (type === 'Capacity') {
      // Find a bigger room
      const biggerRoom = (rooms || [])
        .filter(r => r.capacity > (affected.enrolledCount || 0) && r.id !== affected.classroomId)
        .sort((a, b) => a.capacity - b.capacity)[0]; // smallest room that fits
      if (biggerRoom) {
        return { conflictIndex: i, sectionId: affected.id, fix: 'change_room',
          label: `Move to ${biggerRoom.building} ${biggerRoom.roomNumber} (${biggerRoom.capacity} seats)`,
          payload: { classroomId: biggerRoom.id },
          description: `Move ${affected.courseCode} to ${biggerRoom.building} ${biggerRoom.roomNumber} (${biggerRoom.capacity} seats)` };
      }
    }

    return { conflictIndex: i, sectionId: affected.id, fix: 'manual',
      label: `Manually reassign ${affected.courseCode}`,
      payload: null, description: 'No automatic fix found — edit this section manually.' };
  });
}

// ── Main Component ────────────────────────────────────────────────────────────
const MasterSchedule = () => {
  const [sections, setSections] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isOrchestrating, setIsOrchestrating] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [semesterStatus, setSemesterStatus] = useState(null);
  const [filter, setFilter] = useState('');

  // Conflict state
  const [conflicts, setConflicts] = useState([]);
  const [resolutions, setResolutions] = useState([]);
  const [conflictIds, setConflictIds] = useState(new Set());
  const [showConflictPanel, setShowConflictPanel] = useState(false);
  const [applyingFix, setApplyingFix] = useState(null);
  const [appliedFixes, setAppliedFixes] = useState(new Set());

  // Custom confirm modal
  const [confirmModal, setConfirmModal] = useState({ isOpen: false, title: '', message: '', onConfirm: null });
  const [viewMode, setViewMode] = useState('table'); // 'table' | 'timetable'

  const currentSemester = 'Fall 2026';

  useEffect(() => { init(); }, []);

  const init = async () => {
    setIsLoading(true);
    try {
      const [sectionData, roomData] = await Promise.all([
        sectionService.getAll({ semester: currentSemester }),
        client.get('/classroom').then(r => r.data).catch(() => [])
      ]);
      setSections(sectionData);
      setRooms(roomData);
      await fetchStatus();
    } catch { toast.error('Failed to load schedule data'); }
    finally { setIsLoading(false); }
  };

  const fetchStatus = async () => {
    try {
      const s = await sectionService.getSemesterStatus(currentSemester);
      setSemesterStatus(s);
    } catch (e) { console.error('status error', e); }
  };

  const runScheduler = async () => {
    setIsOrchestrating(true);
    setAppliedFixes(new Set());
    setShowConflictPanel(false);
    try {
      const r = await sectionService.runScheduler(currentSemester);
      if (r.sectionsCreated === 0) toast.warning('0 sections created. Make sure instructors have availability set.');
      else toast.success(`${r.sectionsCreated} draft sections created for ${r.coursesProcessed} courses!`);
      const fresh = await sectionService.getAll({ semester: currentSemester });
      setSections(fresh);
      await fetchStatus();
      // Auto-run conflict check
      runConflictCheck(fresh, rooms);
    } catch (e) {
      toast.error(`Scheduler failed: ${e.response?.data?.message || e.message}`);
    } finally { setIsOrchestrating(false); }
  };

  const runConflictCheck = (sectionList = sections, roomList = rooms) => {
    const { found, ids } = detectConflicts(sectionList);
    const res = generateResolutions(found, sectionList, roomList);
    setConflicts(found);
    setResolutions(res);
    setConflictIds(ids);
    setShowConflictPanel(true);
    if (found.length === 0) toast.success('No conflicts — schedule is clean!');
    else toast.error(`${found.length} conflict${found.length > 1 ? 's' : ''} found. Review suggested fixes below.`);
  };

  const applyFix = async (resolution) => {
    if (!resolution.payload) { toast.info('No automatic fix available. Edit the section manually.'); return; }
    setApplyingFix(resolution.conflictIndex);
    try {
      // Get current section data and merge the fix payload
      const sec = sections.find(s => s.id === resolution.sectionId);
      if (!sec) throw new Error('Section not found');
      await client.put(`/section/${resolution.sectionId}`, {
        courseId: sec.courseId, instructorId: sec.instructorId,
        classroomId: resolution.payload.classroomId ?? sec.classroomId,
        semester: sec.semester,
        daysOfWeek: resolution.payload.daysOfWeek ?? sec.daysOfWeek,
        startTime: sec.startTime, endTime: sec.endTime,
        capacity: sec.capacity,
      });
      toast.success(`Fixed: ${resolution.description}`);
      setAppliedFixes(prev => new Set([...prev, resolution.conflictIndex]));
      // Refresh and re-check conflicts
      const fresh = await sectionService.getAll({ semester: currentSemester });
      setSections(fresh);
      runConflictCheck(fresh, rooms);
    } catch (e) {
      toast.error(`Fix failed: ${e.response?.data?.message || e.message}`);
    } finally { setApplyingFix(null); }
  };

  const applyAllFixes = async () => {
    const applicable = resolutions.filter(r => r.payload && !appliedFixes.has(r.conflictIndex));
    if (applicable.length === 0) { toast.info('No auto-fixes available.'); return; }
    for (const r of applicable) await applyFix(r);
  };

  const doPublish = async () => {
    setIsPublishing(true);
    try {
      const r = await sectionService.publishSchedule(currentSemester);
      toast.success(r?.message || `${currentSemester} Schedule is now LIVE!`);
      const fresh = await sectionService.getAll({ semester: currentSemester });
      setSections(fresh);
      await fetchStatus();
      setShowConflictPanel(false);
    } catch (e) {
      toast.error(`Publish failed: ${e.response?.data?.message || e.response?.data || e.message}`);
    } finally { setIsPublishing(false); }
  };

  const confirmPublish = () => {
    const unresolvedConflicts = conflicts.length - appliedFixes.size;
    setConfirmModal({
      isOpen: true,
      title: 'Publish Schedule',
      danger: unresolvedConflicts > 0,
      message: unresolvedConflicts > 0
        ? `⚠️ There are still ${unresolvedConflicts} unresolved conflicts. Publishing anyway will make the schedule live with conflicts. Are you sure?`
        : `Publish the ${currentSemester} schedule? This makes all draft sections visible to students.`,
      confirmLabel: unresolvedConflicts > 0 ? 'Publish Anyway' : 'Publish',
      onConfirm: () => { setConfirmModal(m => ({ ...m, isOpen: false })); doPublish(); }
    });
  };

  const hasUnpublished = sections.some(s => !s.isPublished);
  const filteredSections = sections.filter(s =>
    (s.courseCode || '').toLowerCase().includes(filter.toLowerCase()) ||
    (s.instructorName || '').toLowerCase().includes(filter.toLowerCase()) ||
    (s.classroomLabel || '').toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <div className="dash-container">
      {/* Custom Confirm Modal */}
      <ConfirmModal
        isOpen={confirmModal.isOpen}
        title={confirmModal.title}
        message={confirmModal.message}
        confirmLabel={confirmModal.confirmLabel}
        danger={confirmModal.danger}
        onConfirm={confirmModal.onConfirm}
        onCancel={() => setConfirmModal(m => ({ ...m, isOpen: false }))}
      />

      {/* Header */}
      <header className="dash-header-layout">
        <div>
          <div className="dash-info-group mb-2">
            <Globe className="dash-icon-subtle auth-accent" />
            <span className="dash-label-data">{currentSemester}</span>
            <span className={`dash-label-subtle px-2 py-0.5 rounded ml-2 ${semesterStatus?.status === 'Published' ? 'text-green-400' : 'text-slate-400'}`}>
              {semesterStatus?.status || 'Loading...'}
            </span>
          </div>
          <h1 className="dash-title">Master Academic Schedule</h1>
          <p className="dash-subtitle">Manage all course sections and publish the semester schedule.</p>
        </div>
        <div className="dash-btn-group">
          <button onClick={runScheduler} disabled={isOrchestrating || isPublishing || isLoading} className="dash-action-button">
            {isOrchestrating ? <span className="dash-state-loading">Running...</span> : <><Play className="dash-icon-primary" /><span>Run Scheduler</span></>}
          </button>
          <button onClick={() => runConflictCheck()} disabled={isLoading || sections.length === 0} className="dash-action-button"
            style={{ color: conflictIds.size > 0 ? '#f87171' : undefined }}>
            <ShieldAlert className="dash-icon-primary" /><span>Check Conflicts {conflictIds.size > 0 ? `(${conflicts.length})` : ''}</span>
          </button>
          <button onClick={confirmPublish} disabled={isPublishing || isOrchestrating || isLoading || !hasUnpublished} className="dash-primary-btn"
            title={!hasUnpublished ? 'Run scheduler first' : 'Publish to students'}>
            {isPublishing ? <span className="dash-state-loading">Publishing...</span> : <><Globe className="dash-icon-primary" /><span>Publish Schedule</span></>}
          </button>
        </div>
      </header>

      {/* Search + View Toggle */}
      <div className="dash-card dash-section-spacer dash-padding-content" style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
        <div className="dash-search-input-wrapper" style={{ flex: 1 }}>
          <Search className="dash-search-icon" />
          <input type="text" placeholder="Search by course, instructor, or room..." className="dash-search-input"
            value={filter} onChange={e => setFilter(e.target.value)} />
        </div>
        <div style={{ display: 'flex', gap: '0.35rem', flexShrink: 0 }}>
          <button onClick={() => setViewMode('table')}
            className={viewMode === 'table' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.8rem', fontSize: '0.82rem' }}>
            <List style={{ width: 14, height: 14 }} /><span>Table</span>
          </button>
          <button onClick={() => setViewMode('timetable')}
            className={viewMode === 'timetable' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.8rem', fontSize: '0.82rem' }}>
            <LayoutGrid style={{ width: 14, height: 14 }} /><span>Timetable</span>
          </button>
        </div>
      </div>

      {/* Conflict Panel */}
      <AnimatePresence>
        {showConflictPanel && (
          <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}
            className="dash-card dash-section-spacer"
            style={{ border: `1px solid ${conflicts.length === 0 ? '#22c55e' : '#ef4444'}`, background: conflicts.length === 0 ? 'rgba(34,197,94,0.05)' : 'rgba(239,68,68,0.05)' }}>

            {/* Panel Header */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
              <div className="dash-info-group">
                {conflicts.length === 0
                  ? <><CheckCircle2 style={{ color: '#22c55e', width: 18, height: 18 }} /><span style={{ color: '#86efac', fontWeight: 600 }}>No conflicts — schedule is clean</span></>
                  : <><ShieldAlert style={{ color: '#ef4444', width: 18, height: 18 }} /><span style={{ color: '#fca5a5', fontWeight: 600 }}>{conflicts.length} Conflict{conflicts.length > 1 ? 's' : ''} Detected</span></>}
              </div>
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                {conflicts.length > 0 && resolutions.some(r => r.payload) && (
                  <button onClick={applyAllFixes} className="dash-primary-btn" style={{ padding: '0.35rem 0.85rem', fontSize: '0.8rem' }}>
                    <Wrench style={{ width: 13, height: 13 }} /><span>Apply All Auto-Fixes</span>
                  </button>
                )}
                <button onClick={() => setShowConflictPanel(false)} style={{ background: 'none', border: 'none', color: '#64748b', cursor: 'pointer' }}>
                  <X style={{ width: 16, height: 16 }} />
                </button>
              </div>
            </div>

            {/* Conflict + Resolution Cards */}
            {conflicts.map((c, i) => {
              const res = resolutions[i];
              const isApplied = appliedFixes.has(i);
              const isApplying = applyingFix === i;
              return (
                <motion.div key={i} initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                  style={{ background: isApplied ? 'rgba(34,197,94,0.08)' : 'rgba(239,68,68,0.08)', border: `1px solid ${isApplied ? 'rgba(34,197,94,0.2)' : 'rgba(239,68,68,0.2)'}`, borderRadius: 8, padding: '0.75rem 1rem', marginBottom: '0.5rem' }}>
                  <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start', flexWrap: 'wrap' }}>
                    {/* Conflict info */}
                    <div style={{ flex: 1, minWidth: 200 }}>
                      <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'center', marginBottom: '0.25rem' }}>
                        <span style={{ background: c.type === 'Instructor' ? '#7c3aed' : c.type === 'Capacity' ? '#d97706' : '#b45309', color: '#fff', fontSize: '0.65rem', fontWeight: 700, padding: '2px 6px', borderRadius: 4 }}>{c.type.toUpperCase()}</span>
                        {isApplied && <span style={{ background: '#166534', color: '#86efac', fontSize: '0.65rem', fontWeight: 700, padding: '2px 6px', borderRadius: 4 }}>FIXED</span>}
                        <span style={{ color: '#fca5a5', fontWeight: 600, fontSize: '0.85rem' }}>{c.message}</span>
                      </div>
                      <p style={{ color: '#94a3b8', fontSize: '0.8rem', margin: 0 }}>{c.detail}</p>
                    </div>

                    {/* Resolution suggestion */}
                    {res && !isApplied && (
                      <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexShrink: 0 }}>
                        <div style={{ textAlign: 'right' }}>
                          <p style={{ color: '#7dd3fc', fontSize: '0.78rem', margin: 0, fontWeight: 500 }}>Suggested fix:</p>
                          <p style={{ color: '#94a3b8', fontSize: '0.75rem', margin: 0 }}>{res.description}</p>
                        </div>
                        {res.payload ? (
                          <button onClick={() => applyFix(res)} disabled={isApplying} className="dash-primary-btn"
                            style={{ padding: '0.35rem 0.75rem', fontSize: '0.78rem', flexShrink: 0, background: '#0369a1' }}>
                            {isApplying ? 'Applying...' : <><Wrench style={{ width: 12, height: 12 }} /><span>Apply</span></>}
                          </button>
                        ) : (
                          <span style={{ color: '#64748b', fontSize: '0.75rem', fontStyle: 'italic' }}>Manual fix needed</span>
                        )}
                      </div>
                    )}
                    {isApplied && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: '#86efac', fontSize: '0.8rem' }}>
                        <CheckCircle2 style={{ width: 14, height: 14 }} /><span>Fixed</span>
                      </div>
                    )}
                  </div>
                </motion.div>
              );
            })}

            {/* Publish prompt after all fixed */}
            {conflicts.length > 0 && appliedFixes.size === conflicts.filter((_,i) => resolutions[i]?.payload).length && appliedFixes.size > 0 && (
              <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} style={{ marginTop: '0.75rem', padding: '0.75rem 1rem', background: 'rgba(99,102,241,0.1)', border: '1px solid rgba(99,102,241,0.3)', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div>
                  <p style={{ color: '#a5b4fc', fontWeight: 600, margin: 0, fontSize: '0.9rem' }}>All auto-fixes applied!</p>
                  <p style={{ color: '#94a3b8', margin: 0, fontSize: '0.8rem' }}>Ready to publish the schedule to students.</p>
                </div>
                <button onClick={confirmPublish} className="dash-primary-btn">
                  <Globe style={{ width: 14, height: 14 }} /><span>Publish Now</span>
                </button>
              </motion.div>
            )}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Sections — Table or Timetable */}
      {viewMode === 'table' ? (
      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <table className="dash-table">
            <thead className="dash-table-head">
              <tr>
                <th className="dash-table-th">Course</th>
                <th className="dash-table-th">Instructor</th>
                <th className="dash-table-th">Schedule</th>
                <th className="dash-table-th">Room</th>
                <th className="dash-table-th">Seats</th>
                <th className="dash-table-th">Status</th>
              </tr>
            </thead>
            <tbody>
              {isLoading
                ? [1,2,3,4,5].map(i => <tr key={i} className="dash-table-tr"><td colSpan="6" className="dash-table-td"><div className="dash-skeleton dash-skeleton-course" /></td></tr>)
                : filteredSections.map(s => {
                    const isConflict = conflictIds.has(s.id);
                    return (
                      <motion.tr key={s.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="dash-table-tr"
                        style={isConflict ? { background: 'rgba(239,68,68,0.07)', borderLeft: '3px solid #ef4444' } : undefined}>
                        <td className="dash-table-td">
                          <div className="dash-layout-vertical">
                            <span className="dash-heading-item" style={{ display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                              {s.courseCode}{isConflict && <ShieldAlert style={{ width: 12, height: 12, color: '#ef4444' }} />}
                            </span>
                            <span className="dash-label-subtle">{s.courseTitle}</span>
                          </div>
                        </td>
                        <td className="dash-table-td">
                          <div className="dash-info-group">
                            <div className="dash-badge-instructor">{s.instructorName?.split(' ').map(n => n[0]).join('')}</div>
                            <span className="dash-label-primary">{s.instructorName}</span>
                          </div>
                        </td>
                        <td className="dash-table-td">
                          <div className="dash-info-group">
                            <Clock className="dash-icon-subtle auth-accent" />
                            <span>{s.daysOfWeek} {String(s.startTime).substring(0,5)}–{String(s.endTime).substring(0,5)}</span>
                          </div>
                        </td>
                        <td className="dash-table-td"><span className="dash-label-primary">{s.classroomLabel}</span></td>
                        <td className="dash-table-td"><span className="dash-label-data">{s.capacity}</span></td>
                        <td className="dash-table-td">
                          {s.isPublished
                            ? <div className="dash-status-scheduled"><CheckCircle2 className="dash-icon-subtle" /><span>Live</span></div>
                            : <div className="dash-info-group"><Lock className="dash-icon-subtle" /><span className="dash-label-subtle">Draft</span></div>}
                        </td>
                      </motion.tr>
                    );
                  })}
              {!isLoading && filteredSections.length === 0 && (
                <tr><td colSpan="6" className="dash-table-empty-notice">No sections yet — run the scheduler to generate a draft schedule.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      ) : (
      /* ──── TIMETABLE GRID VIEW ──── */
      <div className="dash-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '800px' }}>
            <thead>
              <tr>
                <th style={{ width: 70, padding: '0.75rem 0.5rem', textAlign: 'center', color: '#64748b', fontSize: '0.75rem', fontWeight: 600, textTransform: 'uppercase', borderBottom: '1px solid #1e293b', background: '#0f172a' }}>Time</th>
                {TT_DAYS.map(d => (
                  <th key={d} style={{ padding: '0.75rem', textAlign: 'center', color: '#e2e8f0', fontSize: '0.85rem', fontWeight: 600, borderBottom: '1px solid #1e293b', background: '#0f172a', letterSpacing: '0.05em' }}>{d}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {TT_HOURS.map(hour => (
                <tr key={hour}>
                  <td style={{ padding: '0.25rem 0.5rem', textAlign: 'center', verticalAlign: 'top', color: '#64748b', fontSize: '0.75rem', fontWeight: 500, borderRight: '1px solid #1e293b', borderBottom: '1px solid rgba(30,41,59,0.5)', background: '#0f172a', width: 70, height: 64 }}>
                    {`${String(hour).padStart(2,'0')}:00`}
                  </td>
                  {TT_DAYS.map(day => {
                    // All sections that start at this hour on this day
                    const blocks = filteredSections.filter(s =>
                      ttMatchDay(s.daysOfWeek, day) && parseInt(String(s.startTime).split(':')[0]) === hour
                    );
                    return (
                      <td key={`${day}-${hour}`} style={{ padding: 2, verticalAlign: 'top', borderBottom: '1px solid rgba(30,41,59,0.5)', borderRight: '1px solid rgba(30,41,59,0.3)', height: 64 }}>
                        <div style={{ display: 'flex', gap: 2, height: '100%' }}>
                          {blocks.map(s => {
                            const color = ttHash(s.courseCode);
                            const isConflict = conflictIds.has(s.id);
                            return (
                              <motion.div key={s.id} initial={{ opacity: 0, scale: 0.92 }} animate={{ opacity: 1, scale: 1 }}
                                style={{
                                  flex: 1, background: isConflict ? 'rgba(239,68,68,0.15)' : color.bg,
                                  border: `1px solid ${isConflict ? '#ef4444' : color.border}`,
                                  borderLeft: `3px solid ${isConflict ? '#ef4444' : color.border}`,
                                  borderRadius: 6, padding: '0.3rem 0.4rem',
                                  display: 'flex', flexDirection: 'column', gap: 1, minWidth: 0,
                                }}>
                                <span style={{ color: isConflict ? '#fca5a5' : color.text, fontWeight: 700, fontSize: '0.72rem', lineHeight: 1.2, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                  {s.courseCode}{isConflict && ' ⚠'}
                                </span>
                                <span style={{ color: '#94a3b8', fontSize: '0.6rem', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{s.instructorName}</span>
                                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 'auto', color: '#64748b', fontSize: '0.55rem' }}>
                                  <span>{s.classroomLabel}</span>
                                  <span>{s.isPublished ? '●' : '○'}</span>
                                </div>
                              </motion.div>
                            );
                          })}
                        </div>
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {/* Legend */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.6rem', padding: '0.6rem 1rem', borderTop: '1px solid #1e293b', background: '#0f172a', alignItems: 'center' }}>
          <span style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 600, marginRight: '0.5rem' }}>Legend:</span>
          {[...new Set(filteredSections.map(s => s.courseCode))].map(code => {
            const c = ttHash(code);
            return (
              <div key={code} style={{ display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                <div style={{ width: 8, height: 8, borderRadius: 2, background: c.border }} />
                <span style={{ color: '#94a3b8', fontSize: '0.7rem' }}>{code}</span>
              </div>
            );
          })}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', marginLeft: '1rem' }}>
            <div style={{ width: 8, height: 8, borderRadius: 2, background: '#ef4444' }} />
            <span style={{ color: '#94a3b8', fontSize: '0.7rem' }}>Conflict</span>
          </div>
        </div>
      </div>
      )}

      {/* Draft Banner */}
      {sections.some(s => !s.isPublished) && (
        <div className="dash-alert-banner">
          <AlertTriangle className="dash-alert-icon" />
          <div>
            <h4 className="dash-alert-title">Draft Schedule Active</h4>
            <p className="dash-alert-text">Check for conflicts, apply fixes, then click <strong>Publish Schedule</strong> to make it visible to students.</p>
          </div>
        </div>
      )}

      {/* Conflict Analysis Info */}
      <div style={{
        background: 'rgba(15,23,42,0.6)', border: '1px solid #1e293b', borderRadius: 10,
        padding: '1rem 1.25rem', marginTop: '0.5rem'
      }}>
        <h4 style={{ color: '#94a3b8', fontSize: '0.78rem', fontWeight: 600, marginBottom: '0.6rem', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
          <ShieldAlert style={{ width: 13, height: 13 }} /> Conflict Analysis Rules
        </h4>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '0.5rem' }}>
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
            <div style={{ width: 6, height: 6, borderRadius: 2, background: '#7c3aed', marginTop: 5, flexShrink: 0 }} />
            <div>
              <p style={{ color: '#c4b5fd', fontSize: '0.72rem', fontWeight: 600, margin: 0 }}>Instructor Double-Booking</p>
              <p style={{ color: '#64748b', fontSize: '0.68rem', margin: 0, lineHeight: 1.4 }}>Same instructor assigned to 2+ sections at the same day &amp; time.</p>
            </div>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
            <div style={{ width: 6, height: 6, borderRadius: 2, background: '#b45309', marginTop: 5, flexShrink: 0 }} />
            <div>
              <p style={{ color: '#fcd34d', fontSize: '0.72rem', fontWeight: 600, margin: 0 }}>Room Double-Booking</p>
              <p style={{ color: '#64748b', fontSize: '0.68rem', margin: 0, lineHeight: 1.4 }}>Same classroom assigned to 2+ sections at the same day &amp; time.</p>
            </div>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
            <div style={{ width: 6, height: 6, borderRadius: 2, background: '#d97706', marginTop: 5, flexShrink: 0 }} />
            <div>
              <p style={{ color: '#fbbf24', fontSize: '0.72rem', fontWeight: 600, margin: 0 }}>Capacity Overflow</p>
              <p style={{ color: '#64748b', fontSize: '0.68rem', margin: 0, lineHeight: 1.4 }}>Enrolled student count meets or exceeds the section's seat capacity.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MasterSchedule;
