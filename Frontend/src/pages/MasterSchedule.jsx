import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  LayoutGrid, 
  Play, 
  Filter, 
  Search, 
  AlertTriangle, 
  CheckCircle2, 
  Clock, 
  Users,
  ChevronDown,
  Globe,
  Lock,
  ArrowRight
} from 'lucide-react';
import { sectionService } from '../services/sectionService';
import { toast } from 'sonner';

/**
 * MasterSchedule Page (Admin Only)
 * The global overview of all university sections and scheduling status.
 * Features the "Automated Scheduler" trigger and the "Publishing Pipeline".
 */
const MasterSchedule = () => {
  const [sections, setSections] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isOrchestrating, setIsOrchestrating] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [semesterStatus, setSemesterStatus] = useState(null);
  const [filter, setFilter] = useState('');
  
  const currentSemester = "Fall 2026";

  useEffect(() => {
    fetchInitialData();
  }, []);

  const fetchInitialData = async () => {
    setIsLoading(true);
    try {
      await Promise.all([
        fetchMasterSchedule(),
        fetchSemesterStatus()
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchMasterSchedule = async () => {
    try {
      const data = await sectionService.getAll({ semester: currentSemester });
      setSections(data);
    } catch (error) {
      toast.error('Failed to load master schedule');
    }
  };

  const fetchSemesterStatus = async () => {
    try {
      const status = await sectionService.getSemesterStatus(currentSemester);
      setSemesterStatus(status);
    } catch (error) {
      console.error('Error fetching status:', error);
    }
  };

  const runAutoScheduler = async () => {
    setIsOrchestrating(true);
    try {
      await sectionService.runScheduler(currentSemester);
      toast.success('Optimized draft schedule generated successfully!');
      await fetchInitialData();
    } catch (error) {
      toast.error('Scheduling engine encountered conflicts.');
    } finally {
      setIsOrchestrating(false);
    }
  };

  const handlePublish = async () => {
    if (!window.confirm(`Are you sure you want to publish the ${currentSemester} schedule? This will make it visible to all students.`)) return;
    
    setIsPublishing(true);
    try {
      await sectionService.publishSchedule(currentSemester);
      toast.success(`${currentSemester} Schedule is now LIVE!`);
      await fetchInitialData();
    } catch (error) {
      toast.error(error.response?.data || 'Publishing failed.');
    } finally {
      setIsPublishing(false);
    }
  };

  const filteredSections = sections.filter(s => 
    s.courseCode?.toLowerCase().includes(filter.toLowerCase()) ||
    s.instructorName?.toLowerCase().includes(filter.toLowerCase()) ||
    s.classroomLabel?.toLowerCase().includes(filter.toLowerCase())
  );

  const isDraftMode = semesterStatus?.status === 'Draft' || semesterStatus?.status === 'Partial';

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <div className="dash-info-group mb-2">
            <Globe className="dash-icon-subtle auth-accent" />
            <span className="dash-label-data">{currentSemester} Orchestration</span>
            {semesterStatus?.status === 'Published' ? (
              <span className="dash-status-scheduled ml-2">
                <CheckCircle2 className="dash-icon-subtle" />
                Live
              </span>
            ) : (
              <span className="dash-label-subtle bg-slate-800 px-2 py-0.5 rounded ml-2">Draft</span>
            )}
          </div>
          <h1 className="dash-title">Master Academic Schedule</h1>
          <p className="dash-subtitle">Oversee all course sections and room allocations.</p>
        </div>
        
        <div className="dash-btn-group">
          <button 
            onClick={runAutoScheduler}
            disabled={isOrchestrating || isPublishing || isLoading}
            className="dash-action-button"
          >
            {isOrchestrating ? (
              <span className="dash-state-loading">Orchestrating...</span>
            ) : (
              <>
                <Play className="dash-icon-primary" />
                <span>Re-Run Scheduler</span>
              </>
            )}
          </button>
          
          {isDraftMode && (
            <button 
              onClick={handlePublish}
              disabled={isPublishing || isOrchestrating || isLoading}
              className="dash-primary-btn"
            >
              {isPublishing ? (
                <span className="dash-state-loading">Publishing...</span>
              ) : (
                <>
                  <Globe className="dash-icon-primary" />
                  <span>Publish Schedule</span>
                </>
              )}
            </button>
          )}
        </div>
      </header>

      {/* Filters Bar */}
      <div className="dash-card dash-section-spacer dash-padding-content">
        <div className="dash-layout-horizontal">
          <div className="dash-search-input-wrapper dash-spacer-none">
            <Search className="dash-search-icon" />
            <input 
              type="text" 
              placeholder="Search by course, instructor, or room..."
              className="dash-search-input"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            />
          </div>
          <div className="dash-action-group-compact">
            <button className="dash-action-button dash-info-group">
              <Filter className="dash-icon-subtle" />
              <span>Filter By Room</span>
              <ChevronDown className="dash-icon-subtle" />
            </button>
            <button className="dash-action-button dash-info-group">
              <Users className="dash-icon-subtle" />
              <span>Conflict Analysis</span>
            </button>
          </div>
        </div>
      </div>

      {/* Table View */}
      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <table className="dash-table">
            <thead className="dash-table-head">
              <tr>
                <th className="dash-table-th">Course / Section</th>
                <th className="dash-table-th">Instructor</th>
                <th className="dash-table-th">Schedule</th>
                <th className="dash-table-th">Location</th>
                <th className="dash-table-th">Capacity</th>
                <th className="dash-table-th">State</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                [1, 2, 3, 4, 5].map(i => (
                  <tr key={i} className="dash-table-tr">
                    <td colSpan="6" className="dash-table-td">
                      <div className="dash-skeleton dash-skeleton-course" />
                    </td>
                  </tr>
                ))
              ) : filteredSections.map((section) => (
                <motion.tr 
                  key={section.id}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="dash-table-tr"
                >
                  <td className="dash-table-td">
                    <div className="dash-layout-vertical">
                      <span className="dash-heading-item">
                        {section.courseCode}
                      </span>
                      <span className="dash-label-subtle">{section.courseTitle}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <div className="dash-badge-instructor">
                        {section.instructorName?.split(' ').map(n => n[0]).join('')}
                      </div>
                      <span className="dash-label-primary">{section.instructorName}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <Clock className="dash-icon-subtle auth-accent" />
                      <span>{section.daysOfWeek} {section.startTime} - {section.endTime}</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    <span className="dash-label-primary">{section.classroomLabel}</span>
                  </td>
                  <td className="dash-table-td">
                    <div className="dash-info-group">
                      <span className="dash-label-data">{section.capacity} Seats</span>
                    </div>
                  </td>
                  <td className="dash-table-td">
                    {section.isPublished ? (
                      <div className="dash-status-scheduled">
                        <CheckCircle2 className="dash-icon-subtle" />
                        <span>Live</span>
                      </div>
                    ) : (
                      <div className="dash-info-group">
                        <Lock className="dash-icon-subtle text-slate-500" />
                        <span className="text-slate-500 text-xs font-bold uppercase">Draft</span>
                      </div>
                    )}
                  </td>
                </motion.tr>
              ))}
              {!isLoading && filteredSections.length === 0 && (
                <tr>
                  <td colSpan="6" className="dash-table-empty-notice">
                    No sections found matching your search.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      
      {/* Simulation Notice */}
      {isDraftMode && (
        <div className="dash-alert-banner">
          <AlertTriangle className="dash-alert-icon" />
          <div>
            <h4 className="dash-alert-title">Draft Schedule Active</h4>
            <p className="dash-alert-text">
              The orchestration results above are not yet visible to students. Review the assignments carefully, 
              then click <strong>"Publish Schedule"</strong> to finalize the semester.
            </p>
          </div>
        </div>
      )}
    </div>
  );
};

export default MasterSchedule;
