import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { LogIn, Mail, Lock, Eye, EyeOff } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import FormInput from '../components/FormInput';
import FormButton from '../components/FormButton';

/**
 * Login Page
 * Uses modular components and abstracted services for a professional architecture.
 */
const Login = () => {
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const fillCredentials = (email) => {
    setFormData({ email, password: 'Demo123!' });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      await authService.login(formData);
      console.log('Login successful');
      navigate('/dashboard');
    } catch (error) {
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="auth-page flex items-center justify-center min-h-screen relative overflow-hidden bg-slate-900">
      {/* Quick Credentials Side Panel */}
      <div className="absolute left-0 top-0 h-full w-64 bg-slate-800/80 backdrop-blur-md border-r border-slate-700/50 p-6 flex flex-col justify-center gap-4 z-10 shadow-2xl">
        <h3 className="text-white text-lg font-semibold mb-2 flex items-center gap-2">
          <LogIn size={20} className="text-blue-400" />
          Quick Login
        </h3>
        <p className="text-slate-400 text-sm mb-4">Click to auto-fill credentials.</p>
        
        <button 
          type="button"
          onClick={() => fillCredentials('admin@university.edu')}
          className="w-full text-left px-4 py-3 bg-purple-500/10 hover:bg-purple-500/20 border border-purple-500/30 rounded-xl transition-all duration-300 group"
        >
          <div className="text-purple-300 font-medium group-hover:text-purple-200">Admin</div>
          <div className="text-xs text-purple-400/70">admin@university.edu</div>
        </button>

        <button 
          type="button"
          onClick={() => fillCredentials('teacher@university.edu')}
          className="w-full text-left px-4 py-3 bg-blue-500/10 hover:bg-blue-500/20 border border-blue-500/30 rounded-xl transition-all duration-300 group"
        >
          <div className="text-blue-300 font-medium group-hover:text-blue-200">Instructor</div>
          <div className="text-xs text-blue-400/70">teacher@university.edu</div>
        </button>

        <button 
          type="button"
          onClick={() => fillCredentials('student@university.edu')}
          className="w-full text-left px-4 py-3 bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/30 rounded-xl transition-all duration-300 group"
        >
          <div className="text-emerald-300 font-medium group-hover:text-emerald-200">Student</div>
          <div className="text-xs text-emerald-400/70">student@university.edu</div>
        </button>
      </div>
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

