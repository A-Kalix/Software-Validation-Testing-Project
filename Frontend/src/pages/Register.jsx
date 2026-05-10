import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { UserPlus, Mail, Lock, User, Briefcase, GraduationCap } from 'lucide-react';
import { Link } from 'react-router-dom';
import FormInput from '../components/FormInput';
import FormSelect from '../components/FormSelect';
import FormButton from '../components/FormButton';

/**
 * Register Page
 * Implementation follows the exact schema of the RegisterDTO.
 */
const Register = () => {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
    departmentId: '',
    role: ''
  });

  const handleSubmit = (e) => {
    e.preventDefault();
    console.log('Registration attempt:', formData);
  };

  const departments = [
    { value: '11111111-1111-1111-1111-111111111111', label: 'Computer Science' },
    { value: '22222222-2222-2222-2222-222222222222', label: 'Software Engineering' },
    { value: '33333333-3333-3333-3333-333333333333', label: 'Data Science' }
  ];

  const roles = [
    { value: '0', label: 'Student' },
    { value: '1', label: 'Instructor' }
  ];

  return (
    <main className="auth-page">
      <motion.div 
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="auth-card"
      >
        <header className="auth-header">
          <div className="auth-logo-box">
            <UserPlus className="auth-logo-icon" />
          </div>
          <h2 className="auth-title">Create Account</h2>
          <p className="auth-subtitle">Join the university portal today</p>
        </header>

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="auth-row">
            <FormInput 
              label="First Name"
              icon={User}
              placeholder="John"
              value={formData.firstName}
              onChange={(e) => setFormData({...formData, firstName: e.target.value})}
              required
            />
            <FormInput 
              label="Last Name"
              icon={User}
              placeholder="Doe"
              value={formData.lastName}
              onChange={(e) => setFormData({...formData, lastName: e.target.value})}
              required
            />
          </div>

          <FormInput 
            label="Email Address"
            icon={Mail}
            type="email"
            placeholder="john.doe@university.edu"
            value={formData.email}
            onChange={(e) => setFormData({...formData, email: e.target.value})}
            required
          />

          <div className="auth-row">
            <FormInput 
              label="Password"
              icon={Lock}
              type="password"
              placeholder="••••••••"
              value={formData.password}
              onChange={(e) => setFormData({...formData, password: e.target.value})}
              required
            />
            <FormInput 
              label="Confirm"
              icon={Lock}
              type="password"
              placeholder="••••••••"
              value={formData.confirmPassword}
              onChange={(e) => setFormData({...formData, confirmPassword: e.target.value})}
              required
            />
          </div>

          <FormSelect 
            label="Department"
            icon={GraduationCap}
            value={formData.departmentId}
            options={departments}
            onChange={(e) => setFormData({...formData, departmentId: e.target.value})}
            required
          />

          <FormSelect 
            label="User Role"
            icon={Briefcase}
            value={formData.role}
            options={roles}
            onChange={(e) => setFormData({...formData, role: e.target.value})}
            required
          />

          <FormButton type="submit">
            Create Account
          </FormButton>
        </form>

        <footer className="auth-footer">
          <p className="auth-footer-text">
            Already have an account?{' '}
            <Link to="/login" className="auth-link">
              Sign In
            </Link>
          </p>
        </footer>
      </motion.div>
    </main>
  );
};

export default Register;
