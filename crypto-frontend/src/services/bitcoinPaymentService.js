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
  },

  // Manual status check - calls the new endpoint
  async checkTransactionStatusManually(transactionId) {
    try {
      const response = await api.post(`/check-transaction-status/${transactionId}`);
      return response.data;
    } catch (error) {
      console.error('Error checking transaction status manually:', error);
      throw new Error(error.response?.data?.error || 'Failed to check transaction status');
    }
  },

  // Send callback to PSP for completed/failed transaction
  async sendPSPCallback(transactionData, isSuccessful, message = '') {
    try {
      // This would be the PSP callback endpoint - adjust URL as needed
      const pspCallbackUrl = 'https://localhost:7001/api/psp/payment-callback'; // Adjust to your PSP URL

      const callbackData = {
        transactionId: transactionData.transaction_id || transactionData.transactionId,
        paymentId: transactionData.paymentId,
        buyerEmail: transactionData.buyer_email || transactionData.buyerEmail,
        amount: transactionData.amount,
        currency: transactionData.currency1 || transactionData.currency,
        status: isSuccessful ? 'completed' : 'failed',
        telecomServiceId: transactionData.telecom_service_id || transactionData.telecomServiceId,
        completed: isSuccessful,
        message: message || (isSuccessful ? 'Payment completed successfully' : 'Payment failed or was cancelled'),
        timestamp: new Date().toISOString()
      };

      const response = await axios.post(pspCallbackUrl, callbackData, {
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      return response.data;
    } catch (error) {
      console.error('Error sending PSP callback:', error);
      throw new Error(error.response?.data?.error || 'Failed to send PSP callback');
    }
  }
};