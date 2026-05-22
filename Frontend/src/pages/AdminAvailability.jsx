import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { CalendarDays, Users, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import client from '../api/client';
import { userService } from '../services/userService';

const DAY_NAMES = { 0: 'Sun', 1: 'Mon', 2: 'Tue', 3: 'Wed', 4: 'Thu', 5: 'Fri', 6: 'Sat' };
const DAYS = [1, 2, 3, 4, 5]; // Mon-Fri
const TIME_SLOTS = ['08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00'];

const AdminAvailability = () => {
  const [instructors, setInstructors] = useState([]);
  const [selectedInstructor, setSelectedInstructor] = useState(null);
  const [availability, setAvailability] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isFetchingSlots, setIsFetchingSlots] = useState(false);

  useEffect(() => {
    const load = async () => {
      try {
        const users = await userService.getAll();
        const instructorList = users.filter(u => u.role === 'Instructor');
        setInstructors(instructorList);
        if (instructorList.length > 0) {
          setSelectedInstructor(instructorList[0]);
        }
      } catch {
        toast.error('Failed to load instructors');
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, []);

  useEffect(() => {
    if (!selectedInstructor) return;
    const fetchSlots = async () => {
      setIsFetchingSlots(true);
      try {
        const res = await client.get(`/Availability/all?instructorId=${selectedInstructor.id}`);
        setAvailability(res.data);
      } catch {
        toast.error('Failed to load availability for this instructor');
        setAvailability([]);
      } finally {
        setIsFetchingSlots(false);
      }
    };
    fetchSlots();
  }, [selectedInstructor]);

  const isAvailable = (day, time) =>
    availability.some(a => a.dayOfWeek === day && a.startTime === time);

  if (isLoading) {
    return (
      <div className="dash-container">
        <div className="dash-skeleton" style={{ height: '400px', width: '100%' }} />
      </div>
    );
  }

  return (
    <div className="dash-container">
      <header className="dash-header-layout">
        <div>
          <h1 className="dash-title">Instructor Availability</h1>
          <p className="dash-subtitle">View when each instructor has marked themselves as available to teach.</p>
        </div>
      </header>

      {/* Instructor Selector */}
      <div className="dash-card dash-section-spacer">
        <div className="dash-info-group" style={{ flexWrap: 'wrap', gap: '0.5rem' }}>
          <Users style={{ width: '16px', height: '16px', color: 'var(--tw-color-slate-400, #94a3b8)', flexShrink: 0 }} />
          <span className="dash-label-primary" style={{ marginRight: '0.5rem' }}>Select Instructor:</span>
          {instructors.map(inst => (
            <button
              key={inst.id}
              onClick={() => setSelectedInstructor(inst)}
              className={selectedInstructor?.id === inst.id ? 'dash-primary-btn' : 'dash-secondary-btn'}
              style={{ padding: '0.35rem 0.85rem', fontSize: '0.8rem' }}
            >
              {inst.firstName} {inst.lastName}
            </button>
          ))}
        </div>
      </div>

      {/* Timetable Grid */}
      <div className="dash-panel-clean">
        {isFetchingSlots ? (
          <div className="dash-skeleton" style={{ height: '300px', width: '100%' }} />
        ) : (
          <div className="dash-scroll-area">
            <div className="dash-timetable-wrapper">
              {/* Header Row */}
              <div className="dash-grid-calendar dash-table-head">
                <div className="dash-grid-header">Time</div>
                {DAYS.map(d => (
                  <div key={d} className="dash-grid-day-header">{DAY_NAMES[d]}</div>
                ))}
              </div>

              {/* Time Rows */}
              {TIME_SLOTS.map(time => (
                <div key={time} className="dash-grid-calendar dash-table-tr">
                  <div className="dash-grid-cell-label">{time}</div>
                  {DAYS.map(day => {
                    const available = isAvailable(day, time);
                    return (
                      <div key={`${day}-${time}`} className="dash-grid-slot" style={{ cursor: 'default', minHeight: '60px' }}>
                        {available && (
                          <motion.div
                            initial={{ scale: 0.8, opacity: 0 }}
                            animate={{ scale: 1, opacity: 1 }}
                            className="dash-grid-slot-selected"
                          >
                            <CheckCircle2 className="dash-icon-primary auth-accent" />
                          </motion.div>
                        )}
                        {!available && <div className="dash-grid-slot-inner" />}
                      </div>
                    );
                  })}
                </div>
              ))}
            </div>

            {availability.length === 0 && (
              <p className="dash-table-empty-notice">
                {selectedInstructor?.firstName} has not submitted any availability yet.
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminAvailability;
