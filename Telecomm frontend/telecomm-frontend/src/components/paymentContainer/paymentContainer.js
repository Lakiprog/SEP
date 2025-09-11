import React, { useState } from 'react';
import { Card, CardBody, CardHeader, Button, Alert, Row, Col, Spinner } from 'reactstrap';
import CardPaymentForm from '../cardPaymentForm/cardPaymentForm';
import PSPCardPaymentForm from '../pspCardPaymentForm/pspCardPaymentForm';
import axios from 'axios';

const PaymentContainer = ({ selectedPaymentType, selectedPackage, years, onPaymentComplete, onCancel }) => {
  const [paymentData, setPaymentData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleCardPayment = async (transactionId) => {
    setSuccess(true);
    setLoading(false);
    
    // Call parent callback
    if (onPaymentComplete) {
      onPaymentComplete({
        paymentType: 'card',
        transactionId,
        amount: selectedPackage.price * years,
        status: 'success'
      });
    }
  };

  const initiatePayment = async (paymentType) => {
    setLoading(true);
    setError('');

    try {
      const paymentData = {
        subscriptionId: Math.floor(Math.random() * 1000), // Mock subscription ID
        paymentMethod: paymentType,
        amount: selectedPackage.price * years,
        currency: paymentType === 'paypal' ? 'EUR' : 'RSD',
        description: `Subscription to ${selectedPackage.name} for ${years} year(s)`,
        returnUrl: window.location.origin + '/success',
        cancelUrl: window.location.origin + '/cancel'
      };

      // Call Telecom service to initiate payment
      const response = await axios.post('https://localhost:5000/api/packagedeal/payment/initiate', paymentData);

      if (response.data.success) {
        setSuccess(true);
        setLoading(false);
        
        // Call parent callback
        if (onPaymentComplete) {
          onPaymentComplete({
            paymentType,
            paymentId: response.data.paymentId,
            amount: selectedPackage.price * years,
            status: 'success'
          });
        }
      } else {
        setError(response.data.errorMessage || 'Payment initiation failed');
        setLoading(false);
      }
    } catch (err) {
      console.error('Payment initiation error:', err);
      setError('Failed to initiate payment. Please try again.');
      setLoading(false);
    }
  };

  if (success) {
    return (
      <Card className="border-success">
        <CardBody className="text-center">
          <div className="mb-3">✅</div>
          <h4 className="text-success">Payment Initiated Successfully!</h4>
          <p className="text-muted">Your payment is being processed.</p>
        </CardBody>
      </Card>
    );
  }

  if (selectedPaymentType?.id === 'card') {
    return (
      <PSPCardPaymentForm
        selectedPackage={selectedPackage}
        years={years}
        onPay={handleCardPayment}
        onCancel={onCancel}
      />
    );
  }

  const renderPaymentMethod = () => {
    switch (selectedPaymentType?.id) {
      case 'qr':
        return (
          <div className="text-center">
            <div className="mb-3">
              <div className="border p-4 d-inline-block rounded">
                <div className="text-muted">📱</div>
                <div className="text-muted">QR Code</div>
              </div>
            </div>
            <h5>QR Code Payment</h5>
            <p className="text-muted">
              Scan the QR code with your mobile banking app to complete the payment.
            </p>
            <div className="mb-3">
              <strong>Amount:</strong> ${selectedPackage.price * years} RSD
            </div>
            <Button
              color="primary"
              onClick={() => initiatePayment('qr')}
              disabled={loading}
              className="me-2"
            >
              {loading ? <Spinner size="sm" /> : 'Generate QR Code'}
            </Button>
            <Button color="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        );

      case 'bitcoin':
        return (
          <div className="text-center">
            <div className="mb-3">
              <div className="border p-4 d-inline-block rounded">
                <div className="text-muted">₿</div>
                <div className="text-muted">Bitcoin</div>
              </div>
            </div>
            <h5>Bitcoin Payment</h5>
            <p className="text-muted">
              Complete your payment using Bitcoin cryptocurrency.
            </p>
            <div className="mb-3">
              <strong>Amount:</strong> ${selectedPackage.price * years} RSD
            </div>
            <Button
              color="warning"
              onClick={() => initiatePayment('bitcoin')}
              disabled={loading}
              className="me-2"
            >
              {loading ? <Spinner size="sm" /> : 'Pay with Bitcoin'}
            </Button>
            <Button color="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        );

      case 'paypal':
        return (
          <div className="text-center">
            <div className="mb-3">
              <div className="border p-4 d-inline-block rounded">
                <div className="text-muted">🌐</div>
                <div className="text-muted">PayPal</div>
              </div>
            </div>
            <h5>PayPal Payment</h5>
            <p className="text-muted">
              Complete your payment using your PayPal account.
            </p>
            <div className="mb-3">
              <strong>Amount:</strong> €{(selectedPackage.price * years * 0.0085).toFixed(2)} EUR
            </div>
            <Button
              color="info"
              onClick={() => initiatePayment('paypal')}
              disabled={loading}
              className="me-2"
            >
              {loading ? <Spinner size="sm" /> : 'Pay with PayPal'}
            </Button>
            <Button color="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        );

      default:
        return (
          <div className="text-center">
            <p className="text-muted">Please select a payment method.</p>
          </div>
        );
    }
  };

  return (
    <Card>
      <CardHeader className="bg-primary text-white">
        <h4 className="mb-0">
          {selectedPaymentType?.id === 'card' && '💳'}
          {selectedPaymentType?.id === 'qr' && '📱'}
          {selectedPaymentType?.id === 'bitcoin' && '₿'}
          {selectedPaymentType?.id === 'paypal' && '🌐'}
          {' '}
          {selectedPaymentType?.name?.toUpperCase() || selectedPaymentType?.toUpperCase() || 'PAYMENT'} Payment
        </h4>
      </CardHeader>
      <CardBody>
        {error && (
          <Alert color="danger" className="mb-3" fade={false}>
            {error}
          </Alert>
        )}

        <div className="mb-4 p-3 bg-light rounded">
          <h6>📋 Payment Summary</h6>
          <Row>
            <Col md="6">
              <strong>Package:</strong> {selectedPackage?.name}
            </Col>
            <Col md="6">
              <strong>Duration:</strong> {years} year(s)
            </Col>
          </Row>
          <Row className="mt-2">
            <Col md="6">
              <strong>Price per year:</strong> ${selectedPackage?.price}
            </Col>
            <Col md="6">
              <strong>Total Amount:</strong> <span className="text-primary fw-bold">${selectedPackage?.price * years}</span>
            </Col>
          </Row>
        </div>

        {renderPaymentMethod()}
      </CardBody>
    </Card>
  );
};

export default PaymentContainer;
