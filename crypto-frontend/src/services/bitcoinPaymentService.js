import axios from 'axios';

const API_BASE_URL = 'https://localhost:7002/api/bitcoin';

// Configure axios to handle HTTPS with self-signed certificates (for development)
const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

export const bitcoinPaymentService = {
  // Create a new QR code payment
  async createQRPayment(paymentData) {
    try {
      const response = await api.post('/create-qr-payment', paymentData);
      return response.data;
    } catch (error) {
      console.error('Error creating QR payment:', error);
      throw new Error(error.response?.data?.error || 'Failed to create payment');
    }
  },

  // Get payment status
  async getPaymentStatus(paymentId) {
    try {
      const response = await api.get(`/payment-status/${paymentId}`);
      return response.data;
    } catch (error) {
      console.error('Error getting payment status:', error);
      throw new Error(error.response?.data?.error || 'Failed to get payment status');
    }
  },

  // Get transaction info
  async getTransactionInfo(transactionId) {
    try {
      const response = await api.get(`/transaction-info/${transactionId}`);
      return response.data;
    } catch (error) {
      console.error('Error getting transaction info:', error);
      throw new Error(error.response?.data?.error || 'Failed to get transaction info');
    }
  },

  // Check service health
  async checkHealth() {
    try {
      const response = await axios.get('https://localhost:7002/health');
      return response.data;
    } catch (error) {
      console.error('Bitcoin payment service is not available:', error);
      return null;
    }
  }
};