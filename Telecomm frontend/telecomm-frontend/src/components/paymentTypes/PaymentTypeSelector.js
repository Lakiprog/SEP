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
      // Get payment methods from PSP for Telecom client (ID: 1)
      const response = await axios.get('https://localhost:7005/api/psp/merchants/1/payment-methods');
      
      const pspPaymentTypes = response.data.map(paymentType => ({
        id: paymentType.type,
        name: paymentType.name,
        icon: getPaymentIcon(paymentType.type),
        color: getPaymentColor(paymentType.type),
        description: paymentType.description || `Pay with ${paymentType.name}`
      }));
      
      setPaymentTypes(pspPaymentTypes);
    } catch (err) {
      console.error('Error fetching payment types:', err);
      setError('Failed to load payment types');
    } finally {
      setLoading(false);
    }
  };

  const getPaymentIcon = (type) => {
    switch (type.toLowerCase()) {
      case 'card': return '💳';
      case 'qr': return '📱';
      case 'paypal': return '🌐';
      case 'bitcoin': return '₿';
      default: return '💳';
    }
  };

  const getPaymentColor = (type) => {
    switch (type.toLowerCase()) {
      case 'card': return 'primary';
      case 'qr': return 'success';
      case 'paypal': return 'info';
      case 'bitcoin': return 'warning';
      default: return 'primary';
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
