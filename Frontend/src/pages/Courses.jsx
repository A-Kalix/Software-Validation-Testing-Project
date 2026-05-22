import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { BookOpen, Search, Plus, Trash2, Edit2, X } from 'lucide-react';
import { courseService } from '../services/courseService';
import { toast } from 'sonner';
import client from '../api/client';

/**
 * Courses Page — Role-Aware
 * Admin: Full CRUD with create/edit/delete
 * Instructor/Student: Read-only catalog with search
 */
const Courses = () => {
  const raw = localStorage.getItem('user') || '{}';
  const user = (() => { try { return JSON.parse(raw); } catch { return {}; } })();
  const isAdmin = user.role === 'Admin';

  const [courses, setCourses] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCourse, setEditingCourse] = useState(null);
  const [formData, setFormData] = useState({
    courseCode: '',
    title: '',
    description: '',
    credits: 3,
    departmentId: ''
  });

  const fetchCourses = async () => {
    try {
      setIsLoading(true);
      const [coursesData, deptData] = await Promise.all([
        courseService.getAll(),
        client.get('/department').then(r => r.data).catch(() => [])
      ]);
      setCourses(coursesData);
      setDepartments(deptData);
    } catch {
      toast.error('Failed to load courses');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { fetchCourses(); }, []);

  const openCreate = () => {
    setEditingCourse(null);
    setFormData({ courseCode: '', title: '', description: '', credits: 3, departmentId: departments[0]?.id || '' });
    setIsModalOpen(true);
  };

  const openEdit = (course) => {
    setEditingCourse(course);
    setFormData({
      courseCode: course.courseCode,
      title: course.title,
      description: course.description || '',
      credits: course.credits,
      departmentId: course.departmentId
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingCourse) {
        await courseService.update(editingCourse.id, formData);
        toast.success('Course updated!');
      } else {
        await courseService.create(formData);
        toast.success('Course created!');
      }
      setIsModalOpen(false);
      fetchCourses();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Operation failed.');
    }
  };

  const handleDelete = async (id, code) => {
    if (!window.confirm(`Delete course ${code}? This cannot be undone.`)) return;
    try {
      await courseService.delete(id);
      setCourses(prev => prev.filter(c => c.id !== id));
      toast.success('Course deleted.');
    } catch (e) {
      toast.error(e.response?.data?.message || 'Cannot delete — course may have active sections.');
    }
  };

  const filteredCourses = courses.filter(c =>
    c.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
    c.courseCode.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Course Catalog</h1>
          <p className="dash-subtitle">
            {isAdmin ? 'Add, edit, and remove courses from the university catalog.' : 'Browse all available academic offerings.'}
          </p>
        </div>
        {isAdmin && (
          <button onClick={openCreate} className="dash-primary-btn">
            <Plus className="dash-icon-primary" />
            <span>Add Course</span>
          </button>
        )}
      </header>

      <div className="dash-card dash-section-spacer">
        <div className="dash-search-input-wrapper">
          <Search className="dash-search-icon" />
          <input
            type="text"
            placeholder="Search by code or title..."
            className="dash-search-input"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      <div className="dash-courses-grid">
        {isLoading ? (
          [1, 2, 3].map(i => <div key={i} className="dash-card dash-skeleton-course dash-skeleton" />)
        ) : filteredCourses.length === 0 ? (
          <div className="dash-empty-state" style={{ gridColumn: '1 / -1' }}>
            No courses found matching your search.
          </div>
        ) : filteredCourses.map((course) => (
          <motion.div
            key={course.id}
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="dash-card"
          >
            <div className="dash-card-header-layout">
              <div className="dash-feature-icon-wrapper">
                <BookOpen className="dash-feature-icon" />
              </div>
              <span className="dash-badge">{course.credits} Credits</span>
            </div>
            <h3 className="dash-course-card-code">{course.courseCode}</h3>
            <p className="dash-course-card-title">{course.title}</p>
            <p className="dash-course-card-desc">
              {course.description || 'Explore advanced topics in this university course.'}
            </p>
            <div className="dash-card-footer-layout">
              <span className="dash-label-subtle">{course.departmentName}</span>
              {isAdmin && (
                <div className="dash-info-group">
                  <button
                    onClick={() => openEdit(course)}
                    className="dash-action-button"
                    style={{ padding: '4px 8px' }}
                    title="Edit course"
                  >
                    <Edit2 style={{ width: '14px', height: '14px' }} />
                  </button>
                  <button
                    onClick={() => handleDelete(course.id, course.courseCode)}
                    className="dash-action-button"
                    style={{ padding: '4px 8px', color: 'var(--color-red-400)' }}
                    title="Delete course"
                  >
                    <Trash2 style={{ width: '14px', height: '14px' }} />
                  </button>
                </div>
              )}
            </div>
          </motion.div>
        ))}
      </div>

      {/* Create/Edit Modal */}
      <AnimatePresence>
        {isModalOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="dash-modal-overlay"
            onClick={() => setIsModalOpen(false)}
          >
            <motion.div
              initial={{ scale: 0.95, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.95, opacity: 0 }}
              className="dash-modal"
              onClick={e => e.stopPropagation()}
            >
              <div className="dash-modal-header">
                <h2 className="dash-card-title">
                  {editingCourse ? 'Edit Course' : 'Create New Course'}
                </h2>
                <button onClick={() => setIsModalOpen(false)} className="dash-modal-close">
                  <X style={{ width: '20px', height: '20px' }} />
                </button>
              </div>

              <form onSubmit={handleSubmit} className="dash-modal-form">
                <div className="dash-form-row">
                  <div className="dash-form-group">
                    <label className="dash-form-label">Course Code</label>
                    <input
                      type="text"
                      className="dash-form-input"
                      placeholder="e.g. CS301"
                      value={formData.courseCode}
                      onChange={e => setFormData({ ...formData, courseCode: e.target.value })}
                      required
                    />
                  </div>
                  <div className="dash-form-group">
                    <label className="dash-form-label">Credits</label>
                    <input
                      type="number"
                      min="1" max="10"
                      className="dash-form-input"
                      value={formData.credits}
                      onChange={e => setFormData({ ...formData, credits: parseInt(e.target.value) })}
                      required
                    />
                  </div>
                </div>

                <div className="dash-form-group">
                  <label className="dash-form-label">Title</label>
                  <input
                    type="text"
                    className="dash-form-input"
                    placeholder="e.g. Advanced Algorithms"
                    value={formData.title}
                    onChange={e => setFormData({ ...formData, title: e.target.value })}
                    required
                  />
                </div>

                <div className="dash-form-group">
                  <label className="dash-form-label">Description</label>
                  <textarea
                    className="dash-form-input"
                    rows="3"
                    placeholder="Brief course description..."
                    value={formData.description}
                    onChange={e => setFormData({ ...formData, description: e.target.value })}
                  />
                </div>

                <div className="dash-form-group">
                  <label className="dash-form-label">Department</label>
                  <select
                    className="dash-form-input"
                    value={formData.departmentId}
                    onChange={e => setFormData({ ...formData, departmentId: e.target.value })}
                    required
                  >
                    <option value="">Select department...</option>
                    {departments.map(d => (
                      <option key={d.id} value={d.id}>{d.name}</option>
                    ))}
                  </select>
                </div>

                <div className="dash-modal-actions">
                  <button type="button" onClick={() => setIsModalOpen(false)} className="dash-action-button">
                    Cancel
                  </button>
                  <button type="submit" className="dash-primary-btn">
                    {editingCourse ? 'Save Changes' : 'Create Course'}
                  </button>
                </div>
              </form>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default Courses;
