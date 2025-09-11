import React, { useState } from 'react';
import { Card, CardBody, CardHeader, Form, FormGroup, Label, Input, Button, Alert, Row, Col } from 'reactstrap';
import axios from 'axios';

const CardPaymentForm = ({ selectedPackage, years, onPay, onCancel }) => {
  const [formData, setFormData] = useState({
    cardNumber: '',
    cardHolderName: '',
    expiryDate: '',
    securityCode: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [paymentResponse, setPaymentResponse] = useState(null);

  const totalAmount = selectedPackage ? selectedPackage.price * years : 0;

  // const formatCardNumber = (value) => {
  //   // Remove all non-digits
  //   const digits = value.replace(/\D/g, '');
  //   // Add spaces every 4 digits
  //   return digits.replace(/(\d{4})(?=\d)/g, '$1 ');
  // };

  // const formatExpiryDate = (value) => {
  //   // Remove all non-digits
  //   const digits = value.replace(/\D/g, '');
  //   // Add slash after 2 digits
  //   if (digits.length >= 2) {
  //     return digits.substring(0, 2) + '/' + digits.substring(2, 4);
  //   }
  //   return digits;
  // };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const validateForm = () => {
    if (!formData.cardNumber || formData.cardNumber.length !== 16) {
      setError('Card number must be 16 digits');
      return false;
    }
    if (!formData.cardHolderName.trim()) {
      setError('Card holder name is required');
      return false;
    }
    if (!formData.expiryDate || !/^\d{2}\/\d{2}$/.test(formData.expiryDate)) {
      setError('Expiry date must be in MM/YY format');
      return false;
    }
    if (!formData.securityCode || formData.securityCode.length < 3) {
      setError('Security code is required');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    if (!validateForm()) {
      setLoading(false);
      return;
    }

    try {
      // Process payment through Gateway
      const response = await axios.post('https://localhost:5001/api/payment/card-payment', {
        cardNumber: formData.cardNumber,
        cardHolderName: formData.cardHolderName,
        expiryDate: formData.expiryDate,
        securityCode: formData.securityCode,
        amount: totalAmount,
        description: `Subscription to ${selectedPackage.name} for ${years} year(s)`
      });

      if (response.data.success) {
        setPaymentResponse(response.data);
        setSuccess(true);
        setLoading(false);
        
        // Call parent onPay function after successful payment
        setTimeout(() => {
          onPay(response.data.transactionId);
        }, 1500);
      } else {
        setError(response.data.error || 'Payment failed');
        setLoading(false);
      }
    } catch (err) {
      console.error('Payment error:', err);
      setError(err.response?.data?.error || 'Payment processing failed. Please try again.');
      setLoading(false);
    }
  };

  const formatCardNumber = (value) => {
    const v = value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    const matches = v.match(/\d{4,16}/g);
    const match = matches && matches[0] || '';
    const parts = [];
    
    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }
    
    if (parts.length) {
      return parts.join(' ');
    } else {
      return v;
    }
  };

  const formatExpiryDate = (value) => {
    const v = value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    if (v.length >= 2) {
      return v.substring(0, 2) + '/' + v.substring(2, 4);
    }
    return v;
  };

  if (success) {
    return (
      <Card className="border-success">
        <CardBody className="text-center">
          <div className="mb-3">✅</div>
          <h4 className="text-success">Payment Successful!</h4>
          <p className="text-muted">Your subscription has been activated.</p>
          {paymentResponse?.transactionId && (
            <p className="text-muted">Transaction ID: {paymentResponse.transactionId}</p>
          )}
        </CardBody>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="bg-primary text-white">
        <h4 className="mb-0">💳 Credit Card Payment</h4>
      </CardHeader>
      <CardBody>
        {error && (
          <Alert color="danger" className="mb-3">
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
              <strong>Total Amount:</strong> <span className="text-primary fw-bold">${totalAmount}</span>
            </Col>
          </Row>
        </div>

        <Form onSubmit={handleSubmit}>
          <FormGroup>
            <Label for="cardNumber">Card Number</Label>
            <Input
              id="cardNumber"
              name="cardNumber"
              type="text"
              value={formData.cardNumber}
              onChange={(e) => setFormData(prev => ({ ...prev, cardNumber: formatCardNumber(e.target.value) }))}
              placeholder="1234 5678 9012 3456"
              maxLength="19"
              required
            />
          </FormGroup>

          <Row>
            <Col md="6">
              <FormGroup>
                <Label for="cardHolderName">Card Holder Name</Label>
                <Input
                  id="cardHolderName"
                  name="cardHolderName"
                  type="text"
                  value={formData.cardHolderName}
                  onChange={handleInputChange}
                  placeholder="John Doe"
                  required
                />
              </FormGroup>
            </Col>
            <Col md="3">
              <FormGroup>
                <Label for="expiryDate">Expiry Date</Label>
                <Input
                  id="expiryDate"
                  name="expiryDate"
                  type="text"
                  value={formData.expiryDate}
                  onChange={(e) => setFormData(prev => ({ ...prev, expiryDate: formatExpiryDate(e.target.value) }))}
                  placeholder="MM/YY"
                  maxLength="5"
                  required
                />
              </FormGroup>
            </Col>
            <Col md="3">
              <FormGroup>
                <Label for="securityCode">CVV</Label>
                <Input
                  id="securityCode"
                  name="securityCode"
                  type="text"
                  value={formData.securityCode}
                  onChange={handleInputChange}
                  placeholder="123"
                  maxLength="4"
                  required
                />
              </FormGroup>
            </Col>
          </Row>

          <div className="d-flex justify-content-between mt-4">
            <Button
              type="button"
              color="secondary"
              onClick={onCancel}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              color="primary"
              disabled={loading}
            >
              {loading ? 'Processing...' : `Pay $${totalAmount}`}
            </Button>
          </div>

          <div className="mt-3 text-center">
            <small className="text-muted">
              🔒 Your payment information is secure and encrypted
            </small>
          </div>
        </Form>
      </CardBody>
    </Card>
  );
};

export default CardPaymentForm;
