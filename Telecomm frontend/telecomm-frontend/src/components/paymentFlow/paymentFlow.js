import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, CardHeader, Button, Alert, Progress } from 'reactstrap';
import { useSearchParams, useNavigate } from 'react-router-dom';
import axios from 'axios';

const PaymentFlow = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [paymentData, setPaymentData] = useState(null);
  const [transactionStatus, setTransactionStatus] = useState('pending');

  const paymentId = searchParams.get('paymentId');
  const transactionId = searchParams.get('transactionId');

  const steps = [
    { number: 1, title: 'Payment Initiated', description: 'Payment request created in PSP' },
    { number: 2, title: 'Bank Processing', description: 'Payment sent to bank for processing' },
    { number: 3, title: 'PCC Routing', description: 'Payment routed through Payment Card Center' },
    { number: 4, title: 'Issuer Bank', description: 'Payment processed by issuer bank' },
    { number: 5, title: 'Completion', description: 'Payment completed successfully' }
  ];

  useEffect(() => {
    if (paymentId || transactionId) {
      startPaymentFlow();
    }
  }, [paymentId, transactionId]);

  const startPaymentFlow = async () => {
    setLoading(true);
    setError('');

    try {
      // Simulate payment flow steps
      await simulatePaymentFlow();
    } catch (err) {
      setError('Payment flow failed: ' + err.message);
      setLoading(false);
    }
  };

  const simulatePaymentFlow = async () => {
    // Step 1: Payment Initiated
    setCurrentStep(1);
    await delay(1000);

    // Step 2: Bank Processing
    setCurrentStep(2);
    await delay(1500);

    // Step 3: PCC Routing (if different banks)
    setCurrentStep(3);
    await delay(2000);

    // Step 4: Issuer Bank
    setCurrentStep(4);
    await delay(1500);

    // Step 5: Completion
    setCurrentStep(5);
    setTransactionStatus('completed');
    setLoading(false);

    // Redirect to success page after completion
    setTimeout(() => {
      navigate('/payment/success?transactionId=' + (transactionId || paymentId));
    }, 2000);
  };

  const delay = (ms) => {
    return new Promise(resolve => setTimeout(resolve, ms));
  };

  const getStepIcon = (stepNumber, currentStep, status) => {
    if (stepNumber < currentStep) {
      return '✅';
    } else if (stepNumber === currentStep) {
      if (status === 'completed') {
        return '✅';
      } else if (status === 'failed') {
        return '❌';
      } else {
        return '⏳';
      }
    } else {
      return '⭕';
    }
  };

  const getStepColor = (stepNumber, currentStep, status) => {
    if (stepNumber < currentStep) {
      return 'success';
    } else if (stepNumber === currentStep) {
      if (status === 'completed') {
        return 'success';
      } else if (status === 'failed') {
        return 'danger';
      } else {
        return 'primary';
      }
    } else {
      return 'secondary';
    }
  };

  const progressPercentage = (currentStep / steps.length) * 100;

  return (
    <Container className="mt-5">
      <Row className="justify-content-center">
        <Col md="10">
          <Card>
            <CardHeader className="bg-primary text-white text-center">
              <h3 className="mb-0">🏦 Payment Processing</h3>
              <p className="mb-0">Your payment is being processed through our secure banking system</p>
            </CardHeader>
            <CardBody>
              {error && (
                <Alert color="danger" className="mb-4">
                  {error}
                </Alert>
              )}

              <div className="mb-4">
                <div className="d-flex justify-content-between align-items-center mb-2">
                  <span className="text-muted">Progress</span>
                  <span className="text-muted">{Math.round(progressPercentage)}%</span>
                </div>
                <Progress 
                  value={progressPercentage} 
                  color="primary" 
                  className="mb-3"
                  style={{ height: '10px' }}
                />
              </div>

              <Row>
                {steps.map((step, index) => (
                  <Col md="12" key={step.number} className="mb-3">
                    <Card className={`border-${getStepColor(step.number, currentStep, transactionStatus)}`}>
                      <CardBody>
                        <div className="d-flex align-items-center">
                          <div className="me-3" style={{ fontSize: '2rem' }}>
                            {getStepIcon(step.number, currentStep, transactionStatus)}
                          </div>
                          <div className="flex-grow-1">
                            <h5 className={`mb-1 text-${getStepColor(step.number, currentStep, transactionStatus)}`}>
                              Step {step.number}: {step.title}
                            </h5>
                            <p className="text-muted mb-0">{step.description}</p>
                          </div>
                          {step.number === currentStep && loading && (
                            <div className="ms-3">
                              <div className="spinner-border spinner-border-sm text-primary" role="status">
                                <span className="visually-hidden">Loading...</span>
                              </div>
                            </div>
                          )}
                        </div>
                      </CardBody>
                    </Card>
                  </Col>
                ))}
              </Row>

              {currentStep === 5 && transactionStatus === 'completed' && (
                <div className="text-center mt-4">
                  <Alert color="success">
                    <h5 className="alert-heading">🎉 Payment Completed Successfully!</h5>
                    <p className="mb-0">
                      Your payment has been processed successfully. You will be redirected to the success page shortly.
                    </p>
                  </Alert>
                </div>
              )}

              {transactionStatus === 'failed' && (
                <div className="text-center mt-4">
                  <Alert color="danger">
                    <h5 className="alert-heading">❌ Payment Failed</h5>
                    <p className="mb-0">
                      Your payment could not be processed. Please try again or contact support.
                    </p>
                  </Alert>
                  <Button color="primary" onClick={() => navigate('/packages')}>
                    Try Again
                  </Button>
                </div>
              )}

              <div className="mt-4 text-center">
                <small className="text-muted">
                  🔒 This is a secure payment process. Your information is encrypted and protected.
                </small>
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default PaymentFlow;
