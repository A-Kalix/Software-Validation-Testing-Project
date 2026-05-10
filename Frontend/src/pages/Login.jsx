import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { LogIn, Mail, Lock, Eye, EyeOff } from 'lucide-react';
import { Link } from 'react-router-dom';
import authService from '../services/authService';

/**
 * Login Page
 * Uses modular components and abstracted services for a professional architecture.
 */
const Login = () => {
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      const result = await authService.login(formData);
      authService.handleAuthSuccess(result);
      console.log('Login successful:', result);
      // Navigate to dashboard would happen here
    } catch (error) {
      // central error handling is in client.js, but we could add local feedback here
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
            <LogIn className="auth-logo-icon" />
          </div>
          <h2 className="auth-title">Welcome Back</h2>
          <p className="auth-subtitle">Sign in to your academic portal</p>
        </header>

        <form onSubmit={handleSubmit} className="auth-form">
          <FormInput 
            label="Email Address"
            icon={Mail}
            type="email"
            placeholder="name@university.edu"
            value={formData.email}
            onChange={(e) => setFormData({...formData, email: e.target.value})}
            required
          />

          <FormInput 
            label="Password"
            icon={Lock}
            type={showPassword ? "text" : "password"}
            placeholder="••••••••"
            value={formData.password}
            onChange={(e) => setFormData({...formData, password: e.target.value})}
            required
          >
            <button 
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="auth-password-toggle"
            >
              {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
            </button>
          </FormInput>

          <FormButton type="submit" disabled={isLoading}>
            {isLoading ? 'Signing In...' : 'Sign In'}
          </FormButton>
        </form>

        <footer className="auth-footer">
          <p className="auth-footer-text">
            Don't have an account?{' '}
            <Link to="/register" className="auth-link">
              Create an account
            </Link>
          </p>
        </footer>
      </motion.div>
    </main>
  );
};

export default Login;

