import React, { useState, useEffect } from 'react';
import { Card, CardBody, CardTitle, Row, Col, Button, Badge } from 'reactstrap';
import axios from 'axios';

const PaymentTypeSelector = ({ onPaymentSelected, availablePaymentTypes = [] }) => {
  const [paymentTypes, setPaymentTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedPaymentType, setSelectedPaymentType] = useState(null);

  useEffect(() => {
    if (availablePaymentTypes.length > 0) {
      setPaymentTypes(availablePaymentTypes);
      setLoading(false);
    } else {
      fetchPaymentTypes();
    }
  }, [availablePaymentTypes]);

  const fetchPaymentTypes = async () => {
    try {
      // In real implementation, this would call the backend API
      // For now, we'll use mock data
      const mockPaymentTypes = [
        { id: 'card', name: 'Credit Card', icon: '💳', color: 'primary', description: 'Secure payment with credit or debit card' },
        { id: 'qr', name: 'QR Code', icon: '📱', color: 'success', description: 'Scan QR code with mobile banking app' },
        { id: 'paypal', name: 'PayPal', icon: '🌐', color: 'info', description: 'Pay with your PayPal account' },
        { id: 'bitcoin', name: 'Bitcoin', icon: '₿', color: 'warning', description: 'Cryptocurrency payment' }
      ];
      
      setPaymentTypes(mockPaymentTypes);
    } catch (err) {
      console.error('Error fetching payment types:', err);
      setError('Failed to load payment types');
    } finally {
      setLoading(false);
    }
  };

  const handlePaymentTypeSelect = (paymentType) => {
    setSelectedPaymentType(paymentType);
    if (onPaymentSelected) {
      onPaymentSelected(paymentType);
    }
  };

  if (loading) {
    return (
      <div className="text-center p-4">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="mt-2">Loading payment methods...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        <strong>Error:</strong> {error}
      </div>
    );
  }

  return (
    <div>
      <h5 className="mb-3">
        <span className="me-2">💳</span>
        Select Payment Method
      </h5>
      
      <Row>
        {paymentTypes.map((paymentType) => (
          <Col key={paymentType.id} md={6} className="mb-3">
            <Card 
              className={`h-100 cursor-pointer ${
                selectedPaymentType?.id === paymentType.id ? 'border-primary' : ''
              }`}
              onClick={() => handlePaymentTypeSelect(paymentType)}
              style={{ cursor: 'pointer' }}
            >
              <CardBody className="text-center">
                <div className="mb-3">
                  <span style={{ fontSize: '2rem' }}>{paymentType.icon}</span>
                </div>
                <CardTitle tag="h6" className="mb-2">
                  {paymentType.name}
                </CardTitle>
                <p className="text-muted small mb-3">
                  {paymentType.description}
                </p>
                <Badge 
                  color={selectedPaymentType?.id === paymentType.id ? 'primary' : 'secondary'}
                  className="w-100"
                >
                  {selectedPaymentType?.id === paymentType.id ? '✓ Selected' : 'Click to select'}
                </Badge>
              </CardBody>
            </Card>
          </Col>
        ))}
      </Row>

      {selectedPaymentType && (
        <div className="mt-3 p-3 bg-light rounded">
          <h6 className="mb-2">✅ Selected Payment Method</h6>
          <div className="d-flex align-items-center">
            <span className="me-2" style={{ fontSize: '1.5rem' }}>
              {selectedPaymentType.icon}
            </span>
            <div>
              <strong>{selectedPaymentType.name}</strong>
              <br />
              <small className="text-muted">{selectedPaymentType.description}</small>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default PaymentTypeSelector;
