import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { CalendarDays, Clock, CheckCircle2, AlertCircle, Save } from 'lucide-react';
import { toast } from 'sonner';
import { availabilityService } from '../services/availabilityService';

const InstructorAvailability = () => {
  const [availability, setAvailability] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
  const dayMap = { 'Monday': 1, 'Tuesday': 2, 'Wednesday': 3, 'Thursday': 4, 'Friday': 5 };
  const revDayMap = { 1: 'Monday', 2: 'Tuesday', 3: 'Wednesday', 4: 'Thursday', 5: 'Friday' };

  const timeSlots = [
    '08:00', '09:00', '10:00', '11:00', '12:00', 
    '13:00', '14:00', '15:00', '16:00', '17:00'
  ];

  useEffect(() => {
    fetchAvailability();
  }, []);

  const fetchAvailability = async () => {
    try {
      const data = await availabilityService.getMyAvailability();
      // Map API objects to Grid strings "Day-Time"
      const gridSlots = data.map(slot => `${revDayMap[slot.dayOfWeek]}-${slot.startTime}`);
      setAvailability(gridSlots);
    } catch (error) {
      toast.error('Failed to load availability');
    } finally {
      setIsLoading(false);
    }
  };

  const toggleSlot = (day, time) => {
    const slotId = `${day}-${time}`;
    setAvailability(prev => {
      if (prev.includes(slotId)) {
        return prev.filter(s => s !== slotId);
      }
      return [...prev, slotId];
    });
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      // Map Grid strings back to API objects
      const payload = availability.map(slotId => {
        const [day, time] = slotId.split('-');
        return {
          dayOfWeek: dayMap[day],
          startTime: time,
          endTime: `${(parseInt(time.split(':')[0]) + 1).toString().padStart(2, '0')}:00`, // 1 hour slots
          isPreferred: true
        };
      });

      await availabilityService.updateBulk(payload);
      toast.success('Availability preferences saved successfully');
    } catch (error) {
      toast.error('Failed to save preferences');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Teaching Availability</h1>
          <p className="dash-subtitle">Specify your available slots for the upcoming semester.</p>
        </div>
        <button 
          onClick={handleSave}
          disabled={isSaving}
          className="dash-primary-btn dash-btn-action"
        >
          {isSaving ? (
            <span className="dash-state-loading">Saving...</span>
          ) : (
            <>
              <Save className="dash-icon-primary" />
              <span>Save Preferences</span>
            </>
          )}
        </button>
      </header>

      <div className="dash-panel-clean">
        <div className="dash-scroll-area">
          <div className="dash-timetable-wrapper">
            {/* Header Row */}
            <div className="dash-grid-calendar dash-table-head">
              <div className="dash-grid-header">
                Time
              </div>
              {days.map(day => (
                <div key={day} className="dash-grid-day-header">
                  {day}
                </div>
              ))}
            </div>

            {/* Time Rows */}
            {timeSlots.map(time => (
              <div key={time} className="dash-grid-calendar dash-table-tr">
                <div className="dash-grid-cell-label">
                  {time}
                </div>
                {days.map(day => {
                  const isSelected = availability.includes(`${day}-${time}`);
                  return (
                    <div 
                      key={`${day}-${time}`}
                      onClick={() => toggleSlot(day, time)}
                      className="dash-grid-slot"
                      style={{ minHeight: '60px' }}
                    >
                      {isSelected ? (
                        <motion.div 
                          initial={{ scale: 0.8, opacity: 0 }}
                          animate={{ scale: 1, opacity: 1 }}
                          className="dash-grid-slot-selected"
                        >
                          <CheckCircle2 className="dash-icon-primary auth-accent" />
                        </motion.div>
                      ) : (
                        <div className="dash-grid-slot-inner" />
                      )}
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="dash-layout-stretch">
        <div className="dash-card dash-btn-action">
          <h3 className="dash-card-title dash-info-group">
            <AlertCircle className="dash-icon-primary auth-accent" />
            Scheduling Notes
          </h3>
          <ul className="dash-alert-text">
            <li className="dash-info-group">
              <span className="dash-list-item-bullet" />
              Mark all slots where you are physically able to teach.
            </li>
            <li className="dash-info-group">
              <span className="dash-list-item-bullet" />
              The automated system will prioritize slots that minimize room conflicts.
            </li>
            <li className="dash-info-group">
              <span className="dash-list-item-bullet" />
              You can override these selections until the "Lock Date" (Sep 1st).
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default InstructorAvailability;
