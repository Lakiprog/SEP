import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_BANK_API_URL || 'https://localhost:7001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Bank API endpoints
export const bankAPI = {
  // QR Payment processing
  generateQRPayment: (data) => api.post('/bank/qr-payment', data),
  validateQRCode: (data) => api.post('/bank/validate-qr', data),
  
  // Transaction processing  
  processTransaction: (data) => api.post('/bank/process-transaction', data),
  processQRTransaction: (data) => api.post('/bank/process-qr-transaction', data),
  getTransactionStatus: (paymentId) => api.get(`/bank/transaction-status/${paymentId}`),
  
  // Card payment processing
  processCardPayment: (data) => api.post('/bank/process-transaction', data),
};

export default api;
