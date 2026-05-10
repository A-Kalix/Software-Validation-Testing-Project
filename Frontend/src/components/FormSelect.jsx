import React from 'react';

/**
 * FormSelect Component
 * A reusable, semantic select dropdown field.
 */
const FormSelect = ({ 
  label, 
  icon: Icon, 
  value, 
  onChange, 
  options = [], 
  required = false 
}) => {
  return (
    <div className="auth-input-group">
      <label className="auth-label">{label}</label>
      <div className="auth-input-wrapper">
        {Icon && <Icon className="auth-icon" />}
        <select 
          className="auth-input"
          value={value}
          onChange={onChange}
          required={required}
          style={{ appearance: 'none' }}
        >
          <option value="" disabled>Select {label}</option>
          {options.map((opt) => (
            <option key={opt.value} value={opt.value} className="auth-select-option">
              {opt.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
};

export default FormSelect;
