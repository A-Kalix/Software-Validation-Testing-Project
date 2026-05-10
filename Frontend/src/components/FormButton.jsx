import React from 'react';

/**
 * FormButton Component
 * A reusable, semantic button with consistent styling.
 */
const FormButton = ({ children, onClick, type = 'button', disabled = false }) => {
  return (
    <button 
      type={type} 
      onClick={onClick} 
      disabled={disabled}
      className="auth-button"
    >
      {children}
    </button>
  );
};

export default FormButton;
