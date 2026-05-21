import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Building2, Plus, Search, Trash2, Edit2, Users, X, CheckCircle2, AlertCircle } from 'lucide-react';
import { roomService } from '../services/roomService';
import { toast } from 'sonner';

export default function Rooms() {
  const [rooms, setRooms] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filter, setFilter] = useState('');
  const [editingRoom, setEditingRoom] = useState(null);
  
  const [formData, setFormData] = useState({
    building: '',
    roomNumber: '',
    capacity: 30
  });

  useEffect(() => {
    fetchRooms();
  }, []);

  const fetchRooms = async () => {
    try {
      setIsLoading(true);
      const data = await roomService.getAll();
      setRooms(data);
    } catch (error) {
      toast.error("Failed to load classrooms");
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenModal = (room = null) => {
    if (room) {
      setEditingRoom(room);
      setFormData({
        building: room.building,
        roomNumber: room.roomNumber,
        capacity: room.capacity
      });
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
        toast.success("New room added to the orchestrator");
      }
      setIsModalOpen(false);
      fetchRooms();
    } catch (error) {
      toast.error(error.response?.data || "Operation failed");
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Are you sure? This cannot be undone if sections are assigned.")) return;
    try {
      await roomService.delete(id);
      toast.success("Room removed from inventory");
      fetchRooms();
    } catch (error) {
      toast.error(error.response?.data || "Deletion failed");
    }
  };

  const filteredRooms = rooms.filter(r => 
    r.building.toLowerCase().includes(filter.toLowerCase()) ||
    r.roomNumber.toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Classroom Inventory</h1>
          <p className="dash-subtitle">Manage buildings, capacities, and physical resources.</p>
        </div>
        <button 
          onClick={() => handleOpenModal()}
          className="dash-primary-btn dash-btn-action"
        >
          <Plus className="dash-icon-primary" />
          <span>Add New Room</span>
        </button>
      </header>

      {/* Search Bar */}
      <div className="dash-card dash-section-spacer dash-padding-content">
        <div className="dash-layout-horizontal">
          <div className="dash-search-input-wrapper dash-spacer-none">
            <Search className="dash-search-icon" />
            <input 
              type="text" 
              placeholder="Search by building or room number..."
              className="dash-search-input"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Rooms Table */}
      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <table className="dash-table">
            <thead className="dash-table-head">
              <tr>
                <th className="dash-table-th">Building / Block</th>
                <th className="dash-table-th">Room Number</th>
                <th className="dash-table-th">Max Capacity</th>
                <th className="dash-table-th">Utilization</th>
                <th className="dash-table-th dash-table-align-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                [1, 2, 3].map(i => (
                  <tr key={i} className="dash-table-tr">
                    <td colSpan="5" className="dash-table-td">
                      <div className="dash-skeleton dash-skeleton-course" />
                    </td>
                  </tr>
                ))
              ) : filteredRooms.map((room) => (
                <motion.tr 
                  key={room.id}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="dash-table-tr"
                >
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <Building2 className="dash-icon-subtle auth-accent" />
                      <span className="dash-heading-item">{room.building}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-primary">{room.roomNumber}</span>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <Users className="dash-icon-subtle" />
                      <span className="dash-label-data">{room.capacity} Seats</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <div className="dash-progress-track" style={{ width: '100px' }}>
                        <div className="dash-progress-bar" style={{ width: '45%' }} />
                      </div>
                      <span className="dash-label-subtle">45%</span>
                    </div>
                  </td>
                  <td className="dash-table-td dash-table-align-end">
                    <div className="dash-btn-group dash-form-actions dash-action-group-inline">
                      <button 
                        onClick={() => handleOpenModal(room)}
                        className="dash-action-button dash-padding-subtle"
                      >
                        <Edit2 className="dash-icon-subtle" />
                      </button>
                      <button 
                        onClick={() => handleDelete(room.id)}
                        className="dash-danger-icon-btn"
                      >
                        <Trash2 className="dash-icon-subtle" />
                      </button>
                    </div>
                  </td>
                </motion.tr>
              ))}
              {!isLoading && filteredRooms.length === 0 && (
                <tr>
                  <td colSpan="5" className="dash-table-empty-notice">
                    No classrooms found matching your criteria.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add/Edit Modal */}
      <AnimatePresence>
        {isModalOpen && (
          <div className="dash-modal-overlay">
            <motion.div 
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
              className="dash-card dash-modal-content"
            >
              <div className="dash-header-layout dash-spacer-none">
                <h3 className="dash-title">{editingRoom ? 'Edit Classroom' : 'Add New Classroom'}</h3>
                <button onClick={() => setIsModalOpen(false)} className="dash-action-button dash-padding-subtle">
                  <X className="dash-icon-primary" />
                </button>
              </div>

              <form onSubmit={handleSubmit} className="dash-form-layout">
                <div className="dash-layout-vertical">
                  <label className="auth-label-title">Building / Block</label>
                  <input 
                    type="text" 
                    className="dash-search-input"
                    value={formData.building}
                    onChange={(e) => setFormData({...formData, building: e.target.value})}
                    required
                  />
                </div>
                
                <div className="dash-layout-vertical">
                  <label className="auth-label-title">Room Number</label>
                  <input 
                    type="text" 
                    className="dash-search-input"
                    value={formData.roomNumber}
                    onChange={(e) => setFormData({...formData, roomNumber: e.target.value})}
                    required
                  />
                </div>

                <div className="dash-layout-vertical">
                  <label className="auth-label-title">Seating Capacity</label>
                  <input 
                    type="number" 
                    className="dash-search-input"
                    value={formData.capacity}
                    onChange={(e) => setFormData({...formData, capacity: parseInt(e.target.value)})}
                    required
                  />
                </div>

                <div className="dash-form-actions">
                  <button type="submit" className="dash-primary-btn dash-btn-action">
                    <CheckCircle2 className="dash-icon-primary" />
                    <span>{editingRoom ? 'Update Room' : 'Confirm Addition'}</span>
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
