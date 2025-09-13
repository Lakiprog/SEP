import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7006/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized access
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Admin API endpoints
export const adminAPI = {
  // Statistics
  getStatistics: () => api.get('/admin/statistics'),

  // Merchants
  getMerchants: () => api.get('/admin/merchants'),
  getMerchant: (id) => api.get(`/admin/merchants/${id}`),
  createMerchant: (data) => api.post('/admin/merchants', data),
  updateMerchant: (id, data) => api.put(`/admin/merchants/${id}`, data),
  deleteMerchant: (id) => api.delete(`/admin/merchants/${id}`),

  // Payment Methods
  getPaymentMethods: () => api.get('/admin/payment-methods'),
  getPaymentMethod: (id) => api.get(`/admin/payment-methods/${id}`),
  createPaymentMethod: (data) => api.post('/admin/payment-methods', data),
  updatePaymentMethod: (id, data) => api.put(`/admin/payment-methods/${id}`, data),
  deletePaymentMethod: (id) => api.delete(`/admin/payment-methods/${id}`),

  // Merchant Payment Methods
  getMerchantPaymentMethods: (merchantId) => api.get(`/admin/merchants/${merchantId}/payment-methods`),
  addPaymentMethodToMerchant: (merchantId, paymentTypeId) => 
    api.post(`/admin/merchants/${merchantId}/payment-methods`, { paymentTypeId }),
  removePaymentMethodFromMerchant: (merchantId, paymentTypeId) => 
    api.delete(`/admin/merchants/${merchantId}/payment-methods/${paymentTypeId}`),

  // Transactions
  getTransactions: () => api.get('/admin/transactions'),
};

// Payment Selection API endpoints
export const paymentSelectionAPI = {
  getPaymentSelectionPage: (transactionId) => api.get(`/payment-selection/${transactionId}`),
  selectPaymentMethod: (transactionId, data) => api.post(`/payment-selection/${transactionId}/select`, data),
  getMerchantPaymentMethods: (merchantId) => api.get(`/payment-selection/merchant/${merchantId}/payment-methods`),
  getPaymentMethodDetails: (paymentType) => api.get(`/payment-selection/payment-methods/${paymentType}`),
};

// PSP API endpoints
export const pspAPI = {
  createPayment: (data) => api.post('/psp/payment/create', data),
  processPayment: (transactionId, data) => api.post(`/psp/payment/${transactionId}/process`, data),
  getPaymentStatus: (transactionId) => api.get(`/psp/payment/${transactionId}/status`),
  getAvailablePaymentMethods: (merchantId) => api.get(`/psp/payment-methods?merchantId=${merchantId}`),
  handleCallback: (data) => api.post('/psp/callback', data),
  refundPayment: (transactionId, data) => api.post(`/psp/payment/${transactionId}/refund`, data),
  getMerchantTransactions: (merchantId, page = 1, pageSize = 10) => 
    api.get(`/psp/transactions?merchantId=${merchantId}&page=${page}&pageSize=${pageSize}`),
};

// Payment Initiation API endpoints
export const paymentInitiationAPI = {
  initiatePayment: (data) => api.post('/payment/initiate', data),
  getPaymentStatus: (transactionId) => api.get(`/payment/status/${transactionId}`),
};

// WebShop API endpoints
export const webShopAPI = {
  // Authentication
  login: (data) => api.post('/webshop/login', data),
  validateToken: (token) => api.post('/webshop/validate-token', { token }),
  
  // Payment Methods Management
  getAvailablePaymentMethods: (clientId) => api.get(`/webshop/${clientId}/payment-methods`),
  updatePaymentMethods: (clientId, data) => api.post(`/webshop/${clientId}/payment-methods`, data),
  
  // Dashboard
  getDashboard: (clientId) => api.get(`/webshop/${clientId}/dashboard`),
};

// Admin Authentication API endpoints
export const adminAuthAPI = {
  login: (data) => api.post('/admin-auth/login', data),
  validateToken: (token) => api.post('/admin-auth/validate-token', { token }),
  logout: () => api.post('/admin-auth/logout'),
};

export default api;
