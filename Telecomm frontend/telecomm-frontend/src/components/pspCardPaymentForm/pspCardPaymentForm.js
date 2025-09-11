import React, { useState } from 'react';
import { Card, CardBody, CardHeader, Form, FormGroup, Label, Input, Button, Alert, Row, Col, Spinner } from 'reactstrap';
import axios from 'axios';

const PSPCardPaymentForm = ({ selectedPackage, years, onPay, onCancel }) => {
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
  const [currentStep, setCurrentStep] = useState('form'); // form, processing, redirect, success

  const totalAmount = selectedPackage ? selectedPackage.price * years : 0;

  const formatCardNumber = (value) => {
    // Remove all non-digits
    const digits = value.replace(/\D/g, '');
    // Add spaces every 4 digits
    return digits.replace(/(\d{4})(?=\d)/g, '$1 ');
  };

  const formatExpiryDate = (value) => {
    // Remove all non-digits
    const digits = value.replace(/\D/g, '');
    // Add slash after 2 digits
    if (digits.length >= 2) {
      return digits.substring(0, 2) + '/' + digits.substring(2, 4);
    }
    return digits;
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const validateForm = () => {
    // Remove spaces from card number for validation
    const cardNumberDigits = formData.cardNumber.replace(/\s/g, '');
    if (!cardNumberDigits || cardNumberDigits.length !== 16) {
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
    setCurrentStep('processing');

    if (!validateForm()) {
      setLoading(false);
      setCurrentStep('form');
      return;
    }

    try {
      // Step 1: Create payment request in PSP
      const paymentRequest = {
        merchantId: "TELECOM_001",
        merchantPassword: "telecom123",
        amount: totalAmount,
        currency: "RSD",
        merchantOrderID: generateOrderId(),
        description: `Subscription to ${selectedPackage.name} for ${years} year(s)`,
        returnURL: `${window.location.origin}/payment/success`,
        cancelURL: `${window.location.origin}/payment/cancel`,
        callbackURL: `${window.location.origin}/api/payment/callback`,
        customerEmail: "customer@example.com",
        customerName: formData.cardHolderName,
        customData: {
          packageId: selectedPackage.id,
          years: years,
          paymentType: "card"
        }
      };

      console.log('Creating PSP payment request:', paymentRequest);

      // Create payment in PSP
      const createResponse = await axios.post('https://localhost:7006/api/psp/payment/create', paymentRequest);
      
      console.log('PSP Create Response:', createResponse.data);
      
      if (!createResponse.data.success) {
        throw new Error(createResponse.data.message || 'Failed to create payment request');
      }

      const paymentId = createResponse.data.pspTransactionId;
      console.log('PSP Payment created:', paymentId);

      // Step 2: Process payment with card data
      const processRequest = {
        paymentType: "card",
        paymentData: {
          pan: formData.cardNumber.replace(/\s/g, ''), // Remove spaces from card number
          securityCode: formData.securityCode,
          cardHolderName: formData.cardHolderName,
          expiryDate: formData.expiryDate
        }
      };

      console.log('Processing PSP payment through BankService:', processRequest);

      // PSP will route this payment to BankService for processing
      const processResponse = await axios.post(`https://localhost:7006/api/psp/payment/${paymentId}/process`, processRequest);
      
      if (!processResponse.data.success) {
        throw new Error(processResponse.data.message || 'Payment processing failed');
      }

      // Step 3: Check if we need to redirect to bank
      if (processResponse.data.paymentUrl) {
        setCurrentStep('redirect');
        setPaymentResponse(processResponse.data);
        
        // Redirect to bank payment page
        setTimeout(() => {
          window.location.href = processResponse.data.paymentUrl;
        }, 2000);
      } else {
        // Direct success
        setCurrentStep('success');
        setPaymentResponse(processResponse.data);
        setSuccess(true);
        setLoading(false);
        
        // Call parent onPay function after successful payment
        setTimeout(() => {
          onPay(processResponse.data.pspTransactionId);
        }, 1500);
      }

    } catch (err) {
      console.error('PSP Payment error:', err);
      console.error('Error response:', err.response?.data);
      console.error('Error status:', err.response?.status);
      setError(err.response?.data?.message || err.message || 'Payment processing failed. Please try again.');
      setLoading(false);
      setCurrentStep('form');
    }
  };

  const generateOrderId = () => {
    // Generate a proper GUID for PSP
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  };



  // Processing step
  if (currentStep === 'processing') {
    return (
      <Card className="border-info">
        <CardBody className="text-center">
          <div className="mb-3">
            <Spinner color="primary" size="sm" />
          </div>
          <h4 className="text-info">Processing Payment...</h4>
          <p className="text-muted">Please wait while we process your payment through BankService.</p>
          <div className="progress mt-3">
            <div className="progress-bar progress-bar-striped progress-bar-animated" 
                 style={{width: '60%'}}></div>
          </div>
        </CardBody>
      </Card>
    );
  }

  // Redirect step
  if (currentStep === 'redirect') {
    return (
      <Card className="border-warning">
        <CardBody className="text-center">
          <div className="mb-3">🏦</div>
          <h4 className="text-warning">Redirecting to BankService...</h4>
          <p className="text-muted">You will be redirected to BankService for secure payment processing.</p>
          <div className="alert alert-info">
            <strong>Amount:</strong> {totalAmount} RSD<br/>
            <strong>Description:</strong> {selectedPackage.name} subscription
          </div>
          <div className="progress mt-3">
            <div className="progress-bar progress-bar-striped progress-bar-animated bg-warning" 
                 style={{width: '80%'}}></div>
          </div>
          <p className="text-muted mt-2">
            If you are not redirected automatically, 
            <a href={paymentResponse?.paymentUrl} className="btn btn-link p-0">click here</a>
          </p>
        </CardBody>
      </Card>
    );
  }

  // Success step
  if (currentStep === 'success') {
    return (
      <Card className="border-success">
        <CardBody className="text-center">
          <div className="mb-3">✅</div>
          <h4 className="text-success">Payment Successful!</h4>
          <p className="text-muted">Your payment has been processed successfully through BankService.</p>
          {paymentResponse?.pspTransactionId && (
            <div className="alert alert-success">
              <strong>Transaction ID:</strong> {paymentResponse.pspTransactionId}
            </div>
          )}
        </CardBody>
      </Card>
    );
  }

  // Form step (default)
  return (
    <Card>
      <CardHeader className="bg-primary text-white">
        <h4 className="mb-0">💳 Bank Card Payment (PSP)</h4>
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
              <strong>Total Amount:</strong> <span className="text-primary fw-bold">${totalAmount} RSD</span>
            </Col>
          </Row>
        </div>

        <div className="alert alert-info">
          <strong>🔒 Secure Payment:</strong> Your payment will be processed through our secure Payment Service Provider (PSP) system.
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
              {loading ? 'Processing...' : `Pay ${totalAmount} RSD`}
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

export default PSPCardPaymentForm;
