import React from 'react';

/**
 * FormInput Component
 * A reusable, semantic input field with icon support.
 */
const FormInput = ({ 
  label, 
  icon: Icon, 
  type = 'text', 
  placeholder, 
  value, 
  onChange, 
  required = false,
  children 
}) => {
  return (
    <div className="auth-input-group">
      <label className="auth-label">{label}</label>
      <div className="auth-input-wrapper">
        {Icon && <Icon className="auth-icon" />}
        <input 
          type={type}
          placeholder={placeholder}
          className="auth-input"
          value={value}
          onChange={onChange}
          required={required}
        />
        {children}
      </div>
    </div>
  );
};

export default FormInput;
