import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Building2, Plus, Search, Trash2, Edit2, Users, X, CheckCircle2, LayoutGrid, List } from 'lucide-react';
import { roomService } from '../services/roomService';
import { sectionService } from '../services/sectionService';
import { toast } from 'sonner';

export default function Rooms() {
  const [rooms, setRooms] = useState([]);
  const [sections, setSections] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filter, setFilter] = useState('');
  const [editingRoom, setEditingRoom] = useState(null);
  const [viewMode, setViewMode] = useState('table');

  const [formData, setFormData] = useState({
    building: '', roomNumber: '', capacity: 30
  });

  useEffect(() => { fetchData(); }, []);

  const fetchData = async () => {
    try {
      setIsLoading(true);
      const [roomData, sectionData] = await Promise.all([
        roomService.getAll(),
        sectionService.getAll().catch(() => [])
      ]);
      setRooms(roomData);
      setSections(sectionData);
    } catch { toast.error("Failed to load classrooms"); }
    finally { setIsLoading(false); }
  };

  const handleOpenModal = (room = null) => {
    if (room) {
      setEditingRoom(room);
      setFormData({ building: room.building, roomNumber: room.roomNumber, capacity: room.capacity });
    } else {
      setEditingRoom(null);
      setFormData({ building: '', roomNumber: '', capacity: 30 });
    }
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingRoom) {
        await roomService.update(editingRoom.id, { ...formData, id: editingRoom.id });
        toast.success("Room updated successfully");
      } else {
        await roomService.create(formData);
        toast.success("New room added");
      }
      setIsModalOpen(false);
      fetchData();
    } catch (error) {
      toast.error(error.response?.data || "Operation failed");
    }
  };

  const handleDelete = async (id) => {
    try {
      await roomService.delete(id);
      toast.success("Room removed");
      fetchData();
    } catch (error) {
      toast.error(error.response?.data || "Deletion failed — room may be in use");
    }
  };

  // Compute seat capacity utilization (average students enrolled / room capacity)
  const getSeatUtilization = (roomId) => {
    const room = rooms.find(r => r.id === roomId);
    if (!room || room.capacity === 0) return 0;

    const assigned = sections.filter(s => s.classroomId === roomId);
    if (assigned.length === 0) return 0;

    const totalRatio = assigned.reduce((acc, s) => acc + ((s.enrolledCount || 0) / room.capacity), 0);
    return Math.min(100, Math.round((totalRatio / assigned.length) * 100));
  };

  // Compute schedule booking utilization (assigned sections / 50 slots)
  const getScheduleUtilization = (roomId) => {
    const assigned = sections.filter(s => s.classroomId === roomId);
    return Math.min(100, Math.round((assigned.length / 50) * 100));
  };

  const getSectionsForRoom = (roomId) => sections.filter(s => s.classroomId === roomId);

  const filteredRooms = rooms.filter(r =>
    r.building.toLowerCase().includes(filter.toLowerCase()) ||
    r.roomNumber.toLowerCase().includes(filter.toLowerCase())
  );

  const sizeLabel = (cap) => cap >= 100 ? 'Lecture Hall' : cap >= 40 ? 'Large' : cap >= 20 ? 'Medium' : 'Small';
  const sizeColor = (cap) => cap >= 100 ? '#a855f7' : cap >= 40 ? '#3b82f6' : cap >= 20 ? '#22c55e' : '#f59e0b';

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Classroom Inventory</h1>
          <p className="dash-subtitle">{rooms.length} rooms across campus · {sections.length} scheduled sections.</p>
        </div>
        <button onClick={() => handleOpenModal()} className="dash-primary-btn">
          <Plus className="dash-icon-primary" /><span>Add New Room</span>
        </button>
      </header>

      {/* Search + View Toggle */}
      <div className="dash-card dash-section-spacer dash-padding-content" style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
        <div className="dash-search-input-wrapper" style={{ flex: 1 }}>
          <Search className="dash-search-icon" />
          <input type="text" placeholder="Search by building or room number..."
            className="dash-search-input" value={filter} onChange={e => setFilter(e.target.value)} />
        </div>
        <div style={{ display: 'flex', gap: '0.35rem', flexShrink: 0 }}>
          <button onClick={() => setViewMode('table')}
            className={viewMode === 'table' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.8rem', fontSize: '0.82rem' }}>
            <List style={{ width: 14, height: 14 }} /><span>Table</span>
          </button>
          <button onClick={() => setViewMode('grid')}
            className={viewMode === 'grid' ? 'dash-primary-btn' : 'dash-secondary-btn'}
            style={{ padding: '0.4rem 0.8rem', fontSize: '0.82rem' }}>
            <LayoutGrid style={{ width: 14, height: 14 }} /><span>Grid</span>
          </button>
        </div>
      </div>

      {/* TABLE VIEW */}
      {viewMode === 'table' ? (
        <div className="dash-panel-clean">
          <div className="dash-scroll-area">
            <table className="dash-table">
              <thead className="dash-table-head">
                <tr>
                  <th className="dash-table-th">Building</th>
                  <th className="dash-table-th">Room</th>
                  <th className="dash-table-th">Capacity</th>
                  <th className="dash-table-th">Type</th>
                  <th className="dash-table-th">Sections</th>
                  <th className="dash-table-th">Seat Fill</th>
                  <th className="dash-table-th">Booking Rate</th>
                  <th className="dash-table-th dash-table-align-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {isLoading
                  ? [1,2,3].map(i => <tr key={i} className="dash-table-tr"><td colSpan="8" className="dash-table-td"><div className="dash-skeleton dash-skeleton-course" /></td></tr>)
                  : filteredRooms.map(room => {
                    const seatUtil = getSeatUtilization(room.id);
                    const schedUtil = getScheduleUtilization(room.id);
                    const numSections = getSectionsForRoom(room.id).length;
                    return (
                      <motion.tr key={room.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="dash-table-tr">
                        <td className="dash-table-td">
                          <div className="dash-info-group">
                            <Building2 className="dash-icon-subtle auth-accent" />
                            <span className="dash-heading-item">{room.building}</span>
                          </div>
                        </td>
                        <td className="dash-table-td"><span className="dash-label-primary">{room.roomNumber}</span></td>
                        <td className="dash-table-td">
                          <div className="dash-info-group">
                            <Users className="dash-icon-subtle" />
                            <span className="dash-label-data">{room.capacity}</span>
                          </div>
                        </td>
                        <td className="dash-table-td">
                          <span style={{
                            background: `${sizeColor(room.capacity)}20`, color: sizeColor(room.capacity),
                            fontSize: '0.7rem', fontWeight: 600, padding: '2px 8px', borderRadius: 4
                          }}>{sizeLabel(room.capacity)}</span>
                        </td>
                        <td className="dash-table-td"><span className="dash-label-data">{numSections}</span></td>
                        <td className="dash-table-td">
                          <div className="dash-info-group" style={{ gap: '0.5rem' }}>
                            <div style={{ width: 80, height: 6, background: '#1e293b', borderRadius: 3, overflow: 'hidden' }}>
                              <div style={{
                                width: `${seatUtil}%`, height: '100%', borderRadius: 3,
                                background: seatUtil > 80 ? '#ef4444' : seatUtil > 50 ? '#f59e0b' : '#22c55e',
                                transition: 'width 0.3s'
                              }} />
                            </div>
                            <span className="dash-label-subtle" style={{ fontSize: '0.72rem', fontWeight: 600, color: seatUtil > 80 ? '#f87171' : seatUtil > 50 ? '#fbbf24' : '#4ade80' }}>{seatUtil}%</span>
                          </div>
                        </td>
                        <td className="dash-table-td">
                          <div className="dash-info-group" style={{ gap: '0.5rem' }}>
                            <div style={{ width: 80, height: 6, background: '#1e293b', borderRadius: 3, overflow: 'hidden' }}>
                              <div style={{
                                width: `${schedUtil}%`, height: '100%', borderRadius: 3,
                                background: '#818cf8',
                                transition: 'width 0.3s'
                              }} />
                            </div>
                            <span className="dash-label-subtle" style={{ fontSize: '0.72rem', color: '#c7d2fe' }}>{schedUtil}%</span>
                          </div>
                        </td>
                        <td className="dash-table-td dash-table-align-end">
                          <div style={{ display: 'flex', gap: '0.3rem', justifyContent: 'flex-end' }}>
                            <button onClick={() => handleOpenModal(room)} className="dash-action-button" style={{ padding: '0.35rem' }}>
                              <Edit2 style={{ width: 14, height: 14 }} />
                            </button>
                            <button onClick={() => handleDelete(room.id)} className="dash-danger-icon-btn" style={{ padding: '0.35rem' }}>
                              <Trash2 style={{ width: 14, height: 14 }} />
                            </button>
                          </div>
                        </td>
                      </motion.tr>
                    );
                  })}
                {!isLoading && filteredRooms.length === 0 && (
                  <tr><td colSpan="8" className="dash-table-empty-notice">No classrooms found.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        /* GRID / CARD VIEW */
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '0.75rem' }}>
          {isLoading
            ? [1,2,3,4].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: 180 }} />)
            : filteredRooms.map(room => {
              const seatUtil = getSeatUtilization(room.id);
              const schedUtil = getScheduleUtilization(room.id);
              const numSections = getSectionsForRoom(room.id).length;
              const color = sizeColor(room.capacity);
              return (
                <motion.div key={room.id} initial={{ opacity: 0, y: 15 }} animate={{ opacity: 1, y: 0 }}
                  className="dash-card" style={{ borderLeft: `3px solid ${color}`, padding: '1rem 1.25rem' }}>
                  {/* Header */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                    <div>
                      <h3 style={{ color: '#f1f5f9', fontWeight: 700, fontSize: '1rem', margin: 0 }}>{room.building}</h3>
                      <p style={{ color: '#94a3b8', fontSize: '0.85rem', margin: 0 }}>Room {room.roomNumber}</p>
                    </div>
                    <span style={{
                      background: `${color}20`, color, fontSize: '0.68rem', fontWeight: 600,
                      padding: '2px 8px', borderRadius: 4
                    }}>{sizeLabel(room.capacity)}</span>
                  </div>

                  {/* Stats */}
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginBottom: '0.75rem' }}>
                    <div style={{ background: '#0f172a', borderRadius: 6, padding: '0.5rem', textAlign: 'center' }}>
                      <p style={{ color: '#64748b', fontSize: '0.65rem', margin: 0, textTransform: 'uppercase' }}>Capacity</p>
                      <p style={{ color: '#e2e8f0', fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>{room.capacity}</p>
                    </div>
                    <div style={{ background: '#0f172a', borderRadius: 6, padding: '0.5rem', textAlign: 'center' }}>
                      <p style={{ color: '#64748b', fontSize: '0.65rem', margin: 0, textTransform: 'uppercase' }}>Sections</p>
                      <p style={{ color: '#e2e8f0', fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>{numSections}</p>
                    </div>
                  </div>

                  {/* Seat occupancy bar */}
                  <div style={{ marginBottom: '0.65rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.25rem' }}>
                      <span style={{ color: '#64748b', fontSize: '0.7rem' }}>Seat Fill</span>
                      <span style={{ color: seatUtil > 80 ? '#ef4444' : seatUtil > 50 ? '#f59e0b' : '#22c55e', fontSize: '0.7rem', fontWeight: 600 }}>{seatUtil}%</span>
                    </div>
                    <div style={{ width: '100%', height: 5, background: '#1e293b', borderRadius: 3, overflow: 'hidden' }}>
                      <div style={{
                        width: `${seatUtil}%`, height: '100%', borderRadius: 3,
                        background: seatUtil > 80 ? '#ef4444' : seatUtil > 50 ? '#f59e0b' : '#22c55e',
                        transition: 'width 0.3s'
                      }} />
                    </div>
                  </div>

                  {/* Schedule utilization bar */}
                  <div style={{ marginBottom: '1rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.25rem' }}>
                      <span style={{ color: '#64748b', fontSize: '0.7rem' }}>Weekly Booking Rate</span>
                      <span style={{ color: '#c7d2fe', fontSize: '0.7rem', fontWeight: 600 }}>{schedUtil}%</span>
                    </div>
                    <div style={{ width: '100%', height: 5, background: '#1e293b', borderRadius: 3, overflow: 'hidden' }}>
                      <div style={{
                        width: `${schedUtil}%`, height: '100%', borderRadius: 3,
                        background: '#818cf8',
                        transition: 'width 0.3s'
                      }} />
                    </div>
                  </div>

                  {/* Actions */}
                  <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button onClick={() => handleOpenModal(room)} className="dash-secondary-btn" style={{ flex: 1, justifyContent: 'center', padding: '0.35rem' }}>
                      <Edit2 style={{ width: 13, height: 13 }} /><span>Edit</span>
                    </button>
                    <button onClick={() => handleDelete(room.id)}
                      style={{
                        background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.2)',
                        borderRadius: 8, padding: '0.35rem 0.75rem', cursor: 'pointer',
                        color: '#f87171', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.3rem'
                      }}>
                      <Trash2 style={{ width: 13, height: 13 }} />
                    </button>
                  </div>
                </motion.div>
              );
            })}
        </div>
      )}

      {/* Add/Edit Modal */}
      <AnimatePresence>
        {isModalOpen && (
          <div className="dash-modal-overlay">
            <motion.div initial={{ scale: 0.9, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} exit={{ scale: 0.9, opacity: 0 }}
              className="dash-modal-card">
              <div className="dash-modal-header">
                <h3 className="dash-modal-title">{editingRoom ? 'Edit Classroom' : 'Add New Classroom'}</h3>
                <button onClick={() => setIsModalOpen(false)} className="dash-modal-close"><X style={{ width: 18, height: 18 }} /></button>
              </div>
              <form onSubmit={handleSubmit} className="dash-modal-form">
                <div className="dash-input-group">
                  <label className="dash-input-label">Building / Block</label>
                  <input type="text" className="dash-input" placeholder="e.g. Block A"
                    value={formData.building} onChange={e => setFormData({...formData, building: e.target.value})} required />
                </div>
                <div className="dash-input-group">
                  <label className="dash-input-label">Room Number</label>
                  <input type="text" className="dash-input" placeholder="e.g. 201"
                    value={formData.roomNumber} onChange={e => setFormData({...formData, roomNumber: e.target.value})} required />
                </div>
                <div className="dash-input-group">
                  <label className="dash-input-label">Seating Capacity</label>
                  <input type="number" min="1" max="500" className="dash-input"
                    value={formData.capacity} onChange={e => setFormData({...formData, capacity: parseInt(e.target.value)})} required />
                </div>
                <div className="dash-modal-actions">
                  <button type="button" onClick={() => setIsModalOpen(false)} className="dash-secondary-btn">Cancel</button>
                  <button type="submit" className="dash-primary-btn">
                    <CheckCircle2 style={{ width: 14, height: 14 }} />
                    <span>{editingRoom ? 'Update Room' : 'Add Room'}</span>
                  </button>
                </div>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
}
