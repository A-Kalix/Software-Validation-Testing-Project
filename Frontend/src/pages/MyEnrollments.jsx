import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { Clock, Building, GraduationCap, XCircle, BookOpen } from 'lucide-react';
import { enrollmentService } from '../services/enrollmentService';
import { toast } from 'sonner';

const MyEnrollments = () => {
  const [enrollments, setEnrollments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setIsLoading(true);
    try {
      const data = await enrollmentService.getMyEnrollments();
      setEnrollments((data || []).filter(e => e.status?.toLowerCase() === 'active'));
    } catch {
      toast.error('Failed to load your enrolled courses');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDrop = async (enrollment) => {
    setIsProcessing(enrollment.id);
    try {
      await enrollmentService.drop(enrollment.id);
      toast.success(`Dropped ${enrollment.courseCode}`);
      setEnrollments(prev => prev.filter(e => e.id !== enrollment.id));
    } catch {
      toast.error('Failed to drop course');
    } finally {
      setIsProcessing(null);
    }
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">My Enrolled Courses</h1>
          <p className="dash-subtitle">Courses you are currently registered in for this semester.</p>
        </div>
        <div className="dash-info-group">
          <GraduationCap style={{ width: 18, height: 18, color: '#818cf8' }} />
          <span className="dash-label-data">{enrollments.length} course{enrollments.length !== 1 ? 's' : ''}</span>
        </div>
      </header>

      {!isLoading && enrollments.length === 0 && (
        <div className="dash-card" style={{ textAlign: 'center', padding: '4rem' }}>
          <BookOpen style={{ width: 48, height: 48, color: '#475569', margin: '0 auto 1rem' }} />
          <p className="dash-label-primary">You haven't enrolled in any courses yet.</p>
          <p className="dash-label-subtle">Go to the Enroll page to browse available sections.</p>
        </div>
      )}

      <div className="dash-sections-grid">
        {isLoading
          ? [1, 2, 3].map(i => <div key={i} className="dash-card dash-skeleton" style={{ height: '200px' }} />)
          : enrollments.map(e => (
            <motion.div key={e.id} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
              className="dash-card-section-item enrolled">
              <div className="dash-card-flex-header">
                <div>
                  <h3 className="dash-section-code">{e.courseCode}</h3>
                  <p className="dash-section-course-title">{e.courseTitle}</p>
                </div>
                <span className="dash-status-scheduled" style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '999px' }}>
                  Enrolled
                </span>
              </div>

              <div className="dash-card-info-grid" style={{ margin: '1rem 0' }}>
                <div className="dash-info-item">
                  <Clock className="dash-info-icon" />
                  <span className="dash-label-primary">{e.daysOfWeek} · {e.startTime} – {e.endTime}</span>
                </div>
                <div className="dash-info-item">
                  <Building className="dash-info-icon" />
                  <span className="dash-label-primary">{e.classroomName}</span>
                </div>
                <div className="dash-info-item">
                  <GraduationCap className="dash-info-icon" />
                  <span className="dash-label-primary">{e.semester}</span>
                </div>
              </div>

              <button onClick={() => handleDrop(e)} disabled={isProcessing === e.id}
                className="dash-action-button"
                style={{ width: '100%', justifyContent: 'center', color: '#f87171' }}>
                <XCircle style={{ width: 16, height: 16 }} />
                <span>{isProcessing === e.id ? 'Dropping...' : 'Drop Course'}</span>
              </button>
            </motion.div>
          ))}
      </div>
    </div>
  );
};

export default MyEnrollments;
