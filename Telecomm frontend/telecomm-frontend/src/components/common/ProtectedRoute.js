import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

const ProtectedRoute = ({ children, requiredUserType }) => {
  const { isAuthenticated, user, loading, isTokenExpired } = useAuth();

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ height: '50vh' }}>
        <div className="spinner-border" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || isTokenExpired()) {
    return <Navigate to="/login" replace />;
  }

  if (requiredUserType && user?.userType !== requiredUserType) {
    return <Navigate to="/packageDealsUser" replace />;
  }

  return children;
};

export default ProtectedRoute;