import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { CalendarDays, Trash2, Clock, MapPin, GraduationCap, LayoutGrid, List } from 'lucide-react';
import { enrollmentService } from '../services/enrollmentService';
import { toast } from 'sonner';

const MySchedule = () => {
  const [enrollments, setEnrollments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isDropping, setIsDropping] = useState(null);
  const [viewMode, setViewMode] = useState('grid'); // 'grid' or 'list'

  useEffect(() => {
    fetchSchedule();
  }, []);

  const fetchSchedule = async () => {
    try {
      const data = await enrollmentService.getMyEnrollments();
      setEnrollments(data);
    } catch (error) {
      console.error('Error fetching schedule:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDrop = async (enrollmentId) => {
    if (!window.confirm('Are you sure you want to drop this course?')) return;
    
    setIsDropping(enrollmentId);
    try {
      await enrollmentService.drop(enrollmentId);
      setEnrollments(prev => prev.filter(e => e.id !== enrollmentId));
      toast.success('Course dropped successfully');
    } catch (error) {
      toast.error('Could not drop course. Please try again.');
    } finally {
      setIsDropping(null);
    }
  };

  const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'];
  const timeSlots = ['08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00'];

  const getCourseForSlot = (day, time) => {
    return enrollments.find(e => {
      const dayMatches = e.daysOfWeek.includes(day) || 
                        (day === 'Mon' && e.daysOfWeek.includes('M') && !e.daysOfWeek.includes('MW')) || // simple parser
                        (day === 'Wed' && e.daysOfWeek.includes('W')) ||
                        (day === 'Tue' && e.daysOfWeek.includes('T') && !e.daysOfWeek.includes('Th')) ||
                        (day === 'Thu' && e.daysOfWeek.includes('Th'));
      
      // Basic time overlap check (just starts at the hour for simplicity in this visualization)
      const startHour = parseInt(e.startTime.split(':')[0]);
      const slotHour = parseInt(time.split(':')[0]);
      return dayMatches && startHour === slotHour;
    });
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Academic Time Table</h1>
          <p className="dash-subtitle">Your weekly schedule and classroom assignments.</p>
        </div>
        
        <div className="dash-toggle-group">
          <button 
            onClick={() => setViewMode('grid')}
            className={`dash-toggle-btn ${viewMode === 'grid' ? 'active' : ''}`}
          >
            <LayoutGrid className="dash-icon-subtle" />
            <span>Grid</span>
          </button>
          <button 
            onClick={() => setViewMode('list')}
            className={`dash-toggle-btn ${viewMode === 'list' ? 'active' : ''}`}
          >
            <List className="dash-icon-subtle" />
            <span>List</span>
          </button>
        </div>
      </header>

      {isLoading ? (
        <div className="dash-card dash-skeleton-schedule dash-skeleton" />
      ) : enrollments.length === 0 ? (
        <div className="dash-empty-state">
          <CalendarDays className="dash-icon-hero-muted" />
          <p>Your schedule is currently empty.</p>
          <button onClick={() => window.location.href='/dashboard/sections'} className="dash-link">
            Find Classes →
          </button>
        </div>
      ) : viewMode === 'grid' ? (
        <div className="dash-scroll-area">
          <div className="dash-timetable-wrapper">
            {/* Header row with Days */}
            <div className="dash-timetable-header">
              <div className="dash-timetable-time-label-header">Time</div>
              {days.map(day => (
                <div key={day} className="dash-timetable-day">{day}</div>
              ))}
            </div>

            {/* Time Rows */}
            {timeSlots.map(time => (
              <div key={time} className="dash-timetable-row">
                <div className="dash-timetable-time-label">{time}</div>
                {days.map(day => {
                  const course = getCourseForSlot(day, time);
                  return (
                    <div key={`${day}-${time}`} className="dash-timetable-cell">
                      {course && (
                        <motion.div 
                          initial={{ opacity: 0, scale: 0.9 }}
                          animate={{ opacity: 1, scale: 1 }}
                          className="dash-course-block"
                        >
                          <span className="dash-grid-block-code">{course.courseCode}</span>
                          <span className="dash-grid-block-title">{course.courseTitle}</span>
                          <span className="dash-grid-block-info">
                            <MapPin className="dash-grid-block-icon" />
                            {course.classroomName}
                          </span>
                        </motion.div>
                      )}
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="dash-enrollments-list">
          <AnimatePresence>
            {enrollments.map((enrollment) => (
              <motion.div 
                key={enrollment.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 20 }}
                className="dash-list-item-flex"
              >
                <div className="dash-list-item-header">
                  <div className="dash-list-item-icon-wrapper">
                    <GraduationCap className="dash-list-item-icon" />
                  </div>
                  <div>
                    <h3 className="dash-list-item-title">{enrollment.courseTitle}</h3>
                    <div className="dash-list-item-subtitle">
                      <span className="dash-code-badge">{enrollment.courseCode}</span>
                      <span>{enrollment.semester}</span>
                    </div>
                  </div>
                </div>

                <div className="dash-list-item-actions">
                  <div className="dash-info-item">
                    <Clock className="dash-info-icon" />
                    <span>{enrollment.daysOfWeek} {enrollment.startTime} - {enrollment.endTime}</span>
                  </div>
                  <div className="dash-info-item">
                    <MapPin className="dash-info-icon" />
                    <span>{enrollment.classroomName}</span>
                  </div>
                  
                  <button
                    disabled={isDropping === enrollment.id}
                    onClick={() => handleDrop(enrollment.id)}
                    className="dash-danger-icon-btn"
                  >
                    {isDropping === enrollment.id ? (
                      <div className="dash-spinner-danger" />
                    ) : (
                      <Trash2 className="dash-icon-primary" />
                    )}
                  </button>
                </div>
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}
    </div>
  );
};

export default MySchedule;
