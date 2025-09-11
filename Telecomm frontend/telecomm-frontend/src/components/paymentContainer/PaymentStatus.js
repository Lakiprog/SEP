import React from 'react';
import { Alert, Button, Card, CardBody, CardTitle, Row, Col, Badge } from 'reactstrap';

const PaymentStatus = ({ paymentStatus, onRetry, onClose }) => {
  const getStatusIcon = (status) => {
    switch (status.toLowerCase()) {
      case 'success':
        return '✅';
      case 'pending':
        return '⏳';
      case 'failed':
        return '❌';
      case 'processing':
        return '🔄';
      default:
        return '❓';
    }
  };

  const getStatusColor = (status) => {
    switch (status.toLowerCase()) {
      case 'success':
        return 'success';
      case 'pending':
        return 'warning';
      case 'failed':
        return 'danger';
      case 'processing':
        return 'info';
      default:
        return 'secondary';
    }
  };

  const getStatusMessage = (status) => {
    switch (status.toLowerCase()) {
      case 'success':
        return 'Payment completed successfully!';
      case 'pending':
        return 'Payment is being processed. Please wait...';
      case 'failed':
        return 'Payment failed. Please try again.';
      case 'processing':
        return 'Payment is being processed by the bank.';
      default:
        return 'Payment status unknown.';
    }
  };

  if (!paymentStatus) {
    return null;
  }

  return (
    <Card className={`border-${getStatusColor(paymentStatus.status)}`}>
      <CardBody className="text-center">
        <div className="mb-3">
          <span style={{ fontSize: '3rem' }}>
            {getStatusIcon(paymentStatus.status)}
          </span>
        </div>
        
        <CardTitle tag="h4" className={`text-${getStatusColor(paymentStatus.status)}`}>
          {getStatusMessage(paymentStatus.status)}
        </CardTitle>

        <div className="mb-4">
          <Badge color={getStatusColor(paymentStatus.status)} className="fs-6">
            {paymentStatus.status.toUpperCase()}
          </Badge>
        </div>

        <Row className="justify-content-center">
          <Col md={8}>
            <div className="p-3 bg-light rounded">
              <h6>📋 Payment Details</h6>
              <Row>
                <Col md="6">
                  <strong>Payment ID:</strong>
                  <br />
                  <small className="text-muted">{paymentStatus.paymentId}</small>
                </Col>
                <Col md="6">
                  <strong>Last Updated:</strong>
                  <br />
                  <small className="text-muted">
                    {new Date(paymentStatus.lastUpdated).toLocaleString()}
                  </small>
                </Col>
              </Row>
              {paymentStatus.transactionId && (
                <Row className="mt-2">
                  <Col md="12">
                    <strong>Transaction ID:</strong>
                    <br />
                    <small className="text-muted">{paymentStatus.transactionId}</small>
                  </Col>
                </Row>
              )}
            </div>
          </Col>
        </Row>

        <div className="mt-4">
          {paymentStatus.status.toLowerCase() === 'failed' && onRetry && (
            <Button
              color="warning"
              className="me-2"
              onClick={onRetry}
            >
              🔄 Retry Payment
            </Button>
          )}
          
          {onClose && (
            <Button
              color="secondary"
              onClick={onClose}
            >
              Close
            </Button>
          )}
        </div>

        {paymentStatus.status.toLowerCase() === 'success' && (
          <Alert color="success" className="mt-3">
            <strong>🎉 Congratulations!</strong>
            <br />
            Your subscription has been activated successfully.
          </Alert>
        )}

        {paymentStatus.status.toLowerCase() === 'pending' && (
          <Alert color="info" className="mt-3">
            <strong>ℹ️ Information</strong>
            <br />
            Payment processing may take a few minutes. You will receive a confirmation email once completed.
          </Alert>
        )}
      </CardBody>
    </Card>
  );
};

export default PaymentStatus;
