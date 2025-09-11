import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, CardHeader, Form, FormGroup, Label, Input, Button, Alert, Spinner } from 'reactstrap';
import { useSearchParams, useNavigate } from 'react-router-dom';
import axios from 'axios';

const BankPaymentPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    pan: '',
    securityCode: '',
    cardHolderName: '',
    expiryDate: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [paymentInfo, setPaymentInfo] = useState(null);
  const [currentStep, setCurrentStep] = useState('form'); // form, processing, success, failed

  const paymentId = searchParams.get('paymentId');
  const amount = searchParams.get('amount');
  const description = searchParams.get('description');

  useEffect(() => {
    if (!paymentId) {
      setError('Invalid payment request');
      return;
    }

    // Fetch payment information
    fetchPaymentInfo();
  }, [paymentId]);

  const fetchPaymentInfo = async () => {
    try {
      // In real implementation, this would fetch from BankService
      setPaymentInfo({
        paymentId,
        amount: amount || '0',
        description: description || 'Payment',
        merchantName: 'Telecom Operator'
      });
    } catch (err) {
      setError('Failed to load payment information');
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const validateForm = () => {
    if (!formData.pan || formData.pan.length !== 16) {
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
      // Process payment through BankService
      const paymentRequest = {
        paymentId: paymentId,
        cardData: {
          pan: formData.pan,
          securityCode: formData.securityCode,
          cardHolderName: formData.cardHolderName,
          expiryDate: formData.expiryDate
        }
      };

      console.log('Processing bank payment:', paymentRequest);

      const response = await axios.post('https://localhost:7001/api/bank/payment/process', paymentRequest);
      
      if (response.data.redirectUrl) {
        // Redirect to success/failure page
        window.location.href = response.data.redirectUrl;
      } else {
        // Handle direct response
        if (response.data.status === 'completed') {
          setCurrentStep('success');
        } else {
          setCurrentStep('failed');
          setError(response.data.statusMessage || 'Payment failed');
        }
      }

    } catch (err) {
      console.error('Bank payment error:', err);
      setError(err.response?.data?.message || err.message || 'Payment processing failed. Please try again.');
      setCurrentStep('form');
    } finally {
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

  const handleCancel = () => {
    navigate('/payment/cancel');
  };

  // Processing step
  if (currentStep === 'processing') {
    return (
      <Container className="mt-5">
        <Row className="justify-content-center">
          <Col md="6">
            <Card className="border-info">
              <CardBody className="text-center">
                <div className="mb-3">
                  <Spinner color="primary" size="lg" />
                </div>
                <h4 className="text-info">Processing Payment...</h4>
                <p className="text-muted">Please wait while we process your payment.</p>
                <div className="progress mt-3">
                  <div className="progress-bar progress-bar-striped progress-bar-animated" 
                       style={{width: '70%'}}></div>
                </div>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    );
  }

  // Success step
  if (currentStep === 'success') {
    return (
      <Container className="mt-5">
        <Row className="justify-content-center">
          <Col md="6">
            <Card className="border-success">
              <CardBody className="text-center">
                <div className="mb-3">✅</div>
                <h4 className="text-success">Payment Successful!</h4>
                <p className="text-muted">Your payment has been processed successfully.</p>
                <div className="alert alert-success">
                  <strong>Amount:</strong> {paymentInfo?.amount} RSD<br/>
                  <strong>Description:</strong> {paymentInfo?.description}
                </div>
                <Button color="primary" onClick={() => navigate('/')}>
                  Return to Home
                </Button>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    );
  }

  // Failed step
  if (currentStep === 'failed') {
    return (
      <Container className="mt-5">
        <Row className="justify-content-center">
          <Col md="6">
            <Card className="border-danger">
              <CardBody className="text-center">
                <div className="mb-3">❌</div>
                <h4 className="text-danger">Payment Failed</h4>
                <p className="text-muted">Your payment could not be processed.</p>
                {error && (
                  <Alert color="danger" fade={false}>
                    {error}
                  </Alert>
                )}
                <div className="d-flex justify-content-center gap-2">
                  <Button color="primary" onClick={() => setCurrentStep('form')}>
                    Try Again
                  </Button>
                  <Button color="secondary" onClick={handleCancel}>
                    Cancel
                  </Button>
                </div>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    );
  }

  // Form step (default)
  return (
    <Container className="mt-5">
      <Row className="justify-content-center">
        <Col md="8">
          <Card>
            <CardHeader className="bg-primary text-white text-center">
              <h3 className="mb-0">🏦 Secure Bank Payment</h3>
              <p className="mb-0">Enter your card details to complete the payment</p>
            </CardHeader>
            <CardBody>
              {error && (
                <Alert color="danger" className="mb-3" fade={false}>
                  {error}
                </Alert>
              )}

              {paymentInfo && (
                <div className="mb-4 p-3 bg-light rounded">
                  <h6>📋 Payment Information</h6>
                  <Row>
                    <Col md="6">
                      <strong>Merchant:</strong> {paymentInfo.merchantName}
                    </Col>
                    <Col md="6">
                      <strong>Amount:</strong> <span className="text-primary fw-bold">{paymentInfo.amount} RSD</span>
                    </Col>
                  </Row>
                  <Row className="mt-2">
                    <Col md="12">
                      <strong>Description:</strong> {paymentInfo.description}
                    </Col>
                  </Row>
                </div>
              )}

              <div className="alert alert-warning">
                <strong>🔒 Security Notice:</strong> This is a secure bank payment page. Your card information is encrypted and protected.
              </div>

              <Form onSubmit={handleSubmit}>
                <FormGroup>
                  <Label for="pan">Card Number (PAN)</Label>
                  <Input
                    id="pan"
                    name="pan"
                    type="text"
                    value={formData.pan}
                    onChange={(e) => setFormData(prev => ({ ...prev, pan: formatCardNumber(e.target.value) }))}
                    placeholder="1234 5678 9012 3456"
                    maxLength="19"
                    required
                  />
                </FormGroup>

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

                <Row>
                  <Col md="6">
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
                  <Col md="6">
                    <FormGroup>
                      <Label for="securityCode">Security Code (CVV)</Label>
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
                    onClick={handleCancel}
                    disabled={loading}
                  >
                    Cancel Payment
                  </Button>
                  <Button
                    type="submit"
                    color="primary"
                    disabled={loading}
                    size="lg"
                  >
                    {loading ? 'Processing...' : `Pay ${paymentInfo?.amount || '0'} RSD`}
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
        </Col>
      </Row>
    </Container>
  );
};

export default BankPaymentPage;
