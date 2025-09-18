import React, { createContext, useContext, useState, useEffect } from 'react';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is logged in on app start
    const storedToken = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (storedToken && storedUser) {
      try {
        const userData = JSON.parse(storedUser);

        // Check if token is expired
        if (userData.expiresAt && new Date(userData.expiresAt) > new Date()) {
          setToken(storedToken);
          setUser(userData);
          setIsAuthenticated(true);
        } else {
          // Token expired, clear storage
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
      } catch (error) {
        // Invalid stored data, clear storage
        localStorage.removeItem('token');
        localStorage.removeItem('user');
      }
    }

    setLoading(false);
  }, []);

  const login = (loginResponse) => {
    const { token, userType, username, email, expiresAt } = loginResponse;

    const userData = {
      userType,
      username,
      email,
      expiresAt
    };

    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(userData));

    setToken(token);
    setUser(userData);
    setIsAuthenticated(true);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');

    setToken(null);
    setUser(null);
    setIsAuthenticated(false);
  };

  const isTokenExpired = () => {
    if (!user || !user.expiresAt) return true;
    return new Date(user.expiresAt) <= new Date();
  };

  const value = {
    isAuthenticated,
    user,
    token,
    loading,
    login,
    logout,
    isTokenExpired
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};