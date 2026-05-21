import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { UserPlus, Mail, Lock, User, Briefcase, GraduationCap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { authService } from '../services/authService';
import { DEPARTMENTS, USER_ROLES } from '../utils/constants';
import FormInput from '../components/FormInput';
import FormButton from '../components/FormButton';
import FormSelect from '../components/FormSelect';

/**
 * Register Page
 * Implementation follows a clean architecture by offloading logic to services and constants.
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
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      const result = await authService.register(formData);
      console.log('Registration successful:', result);
      // Navigate to login or auto-login
    } catch (error) {
      // centralized handling in client.js
    } finally {
      setIsLoading(false);
    }
  };

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
            options={DEPARTMENTS}
            onChange={(e) => setFormData({...formData, departmentId: e.target.value})}
            required
          />

          <FormSelect 
            label="User Role"
            icon={Briefcase}
            value={formData.role}
            options={USER_ROLES}
            onChange={(e) => setFormData({...formData, role: e.target.value})}
            required
          />

          <FormButton type="submit" disabled={isLoading}>
            {isLoading ? 'Creating Account...' : 'Create Account'}
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
