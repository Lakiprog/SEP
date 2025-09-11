import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, Button, Alert } from 'reactstrap';
import { useSearchParams, useNavigate } from 'react-router-dom';

const PaymentSuccessPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [paymentDetails, setPaymentDetails] = useState(null);

  const transactionId = searchParams.get('transactionId');
  const amount = searchParams.get('amount');
  const description = searchParams.get('description');

  useEffect(() => {
    // Set payment details from URL parameters
    setPaymentDetails({
      transactionId: transactionId || 'N/A',
      amount: amount || '0',
      description: description || 'Payment completed',
      timestamp: new Date().toLocaleString()
    });
  }, [transactionId, amount, description]);

  const handleReturnHome = () => {
    navigate('/');
  };

  const handleViewPackages = () => {
    navigate('/packages');
  };

  return (
    <Container className="mt-5">
      <Row className="justify-content-center">
        <Col md="8">
          <Card className="border-success">
            <CardBody className="text-center">
              <div className="mb-4" style={{ fontSize: '5rem' }}>
                ✅
              </div>
              
              <h1 className="text-success mb-3">Payment Successful!</h1>
              
              <p className="lead text-muted mb-4">
                Your payment has been processed successfully. Your subscription is now active!
              </p>

              {paymentDetails && (
                <div className="mb-4">
                  <Card className="bg-light">
                    <CardBody>
                      <h5 className="text-primary mb-3">📋 Payment Details</h5>
                      <Row>
                        <Col md="6">
                          <strong>Transaction ID:</strong><br/>
                          <small className="text-muted">{paymentDetails.transactionId}</small>
                        </Col>
                        <Col md="6">
                          <strong>Amount:</strong><br/>
                          <small className="text-muted">{paymentDetails.amount} RSD</small>
                        </Col>
                      </Row>
                      <Row className="mt-2">
                        <Col md="6">
                          <strong>Description:</strong><br/>
                          <small className="text-muted">{paymentDetails.description}</small>
                        </Col>
                        <Col md="6">
                          <strong>Date:</strong><br/>
                          <small className="text-muted">{paymentDetails.timestamp}</small>
                        </Col>
                      </Row>
                    </CardBody>
                  </Card>
                </div>
              )}

              <Alert color="success" className="mb-4" fade={false}>
                <h5 className="alert-heading">🎉 Congratulations!</h5>
                <p className="mb-0">
                  Your subscription has been activated successfully. You can now access all the features 
                  of your selected package. A confirmation email has been sent to your registered email address.
                </p>
              </Alert>

              <div className="d-flex justify-content-center gap-3 flex-wrap">
                <Button color="success" size="lg" onClick={handleReturnHome}>
                  🏠 Return to Home
                </Button>
                <Button color="primary" size="lg" onClick={handleViewPackages}>
                  📦 View Packages
                </Button>
              </div>

              <div className="mt-4">
                <small className="text-muted">
                  Need help? Contact our customer support team at support@telecom.com
                </small>
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default PaymentSuccessPage;
