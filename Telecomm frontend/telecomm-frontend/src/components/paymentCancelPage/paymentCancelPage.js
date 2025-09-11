import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, Button, Alert } from 'reactstrap';
import { useSearchParams, useNavigate } from 'react-router-dom';

const PaymentCancelPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [paymentDetails, setPaymentDetails] = useState(null);

  const transactionId = searchParams.get('transactionId');
  const amount = searchParams.get('amount');
  const reason = searchParams.get('reason');

  useEffect(() => {
    // Set payment details from URL parameters
    setPaymentDetails({
      transactionId: transactionId || 'N/A',
      amount: amount || '0',
      reason: reason || 'Payment was cancelled by user',
      timestamp: new Date().toLocaleString()
    });
  }, [transactionId, amount, reason]);

  const handleTryAgain = () => {
    navigate('/packages');
  };

  const handleReturnHome = () => {
    navigate('/');
  };

  const handleContactSupport = () => {
    // In a real app, this would open a support ticket or chat
    window.open('mailto:support@telecom.com?subject=Payment Issue', '_blank');
  };

  return (
    <Container className="mt-5">
      <Row className="justify-content-center">
        <Col md="8">
          <Card className="border-warning">
            <CardBody className="text-center">
              <div className="mb-4" style={{ fontSize: '5rem' }}>
                🚫
              </div>
              
              <h1 className="text-warning mb-3">Payment Cancelled</h1>
              
              <p className="lead text-muted mb-4">
                Your payment has been cancelled. No charges have been made to your account.
              </p>

              {paymentDetails && (
                <div className="mb-4">
                  <Card className="bg-light">
                    <CardBody>
                      <h5 className="text-warning mb-3">📋 Payment Details</h5>
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
                          <strong>Reason:</strong><br/>
                          <small className="text-muted">{paymentDetails.reason}</small>
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

              <Alert color="info" className="mb-4" fade={false}>
                <h5 className="alert-heading">ℹ️ What happened?</h5>
                <p className="mb-0">
                  Your payment was cancelled. This could be due to various reasons such as:
                  insufficient funds, card declined, or you chose to cancel the transaction.
                  No charges have been made to your account.
                </p>
              </Alert>

              <div className="d-flex justify-content-center gap-3 flex-wrap">
                <Button color="primary" size="lg" onClick={handleTryAgain}>
                  🔄 Try Again
                </Button>
                <Button color="secondary" size="lg" onClick={handleReturnHome}>
                  🏠 Return to Home
                </Button>
                <Button color="info" size="lg" onClick={handleContactSupport}>
                  📞 Contact Support
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

export default PaymentCancelPage;
