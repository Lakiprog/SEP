import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, Button, Alert, Spinner } from 'reactstrap';
import { useSearchParams, useNavigate } from 'react-router-dom';
import axios from 'axios';

const PaymentStatusPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [paymentStatus, setPaymentStatus] = useState(null);
  const [transactionDetails, setTransactionDetails] = useState(null);

  const paymentId = searchParams.get('paymentId');
  const transactionId = searchParams.get('transactionId');
  const status = searchParams.get('status');

  useEffect(() => {
    if (paymentId || transactionId) {
      fetchPaymentStatus();
    } else {
      setError('Invalid payment reference');
      setLoading(false);
    }
  }, [paymentId, transactionId]);

  const fetchPaymentStatus = async () => {
    try {
      setLoading(true);
      
      // Try to fetch from PSP first
      if (paymentId) {
        try {
          const response = await axios.get(`https://localhost:7005/api/psp/payment/${paymentId}/status`);
          if (response.data.success) {
            setPaymentStatus(response.data);
            setTransactionDetails(response.data.transaction);
            setLoading(false);
            return;
          }
        } catch (err) {
          console.log('PSP status check failed, trying BankService...');
        }
      }

      // Try BankService if PSP fails
      if (transactionId) {
        try {
          const response = await axios.get(`https://localhost:7001/api/bank/payment/status/${transactionId}`);
          setPaymentStatus(response.data);
          setTransactionDetails(response.data);
          setLoading(false);
          return;
        } catch (err) {
          console.log('BankService status check failed');
        }
      }

      // Fallback to URL parameters
      if (status) {
        setPaymentStatus({
          status: status,
          message: getStatusMessage(status)
        });
        setLoading(false);
        return;
      }

      setError('Unable to retrieve payment status');
      setLoading(false);

    } catch (err) {
      console.error('Error fetching payment status:', err);
      setError('Failed to load payment status');
      setLoading(false);
    }
  };

  const getStatusMessage = (status) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'success':
        return 'Your payment has been processed successfully.';
      case 'failed':
      case 'error':
        return 'Your payment could not be processed.';
      case 'pending':
        return 'Your payment is being processed.';
      case 'cancelled':
        return 'Your payment was cancelled.';
      default:
        return 'Payment status unknown.';
    }
  };

  const getStatusIcon = (status) => {
    switch (status?.toLowerCase()) {
      case 'completed':
      case 'success':
        return '✅';
      case 'failed':
      case 'error':
        return '❌';
      case 'pending':
        return '⏳';
      case 'cancelled':
        return '🚫';
      default:
        return '❓';
    }
  };

  const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
      case 'completed':
      case 'success':
        return 'success';
      case 'failed':
      case 'error':
        return 'danger';
      case 'pending':
        return 'warning';
      case 'cancelled':
        return 'secondary';
      default:
        return 'info';
    }
  };

  const handleRetryPayment = () => {
    navigate('/packages');
  };

  const handleReturnHome = () => {
    navigate('/');
  };

  if (loading) {
    return (
      <Container className="mt-5">
        <Row className="justify-content-center">
          <Col md="6">
            <Card className="border-info">
              <CardBody className="text-center">
                <div className="mb-3">
                  <Spinner color="primary" size="lg" />
                </div>
                <h4 className="text-info">Loading Payment Status...</h4>
                <p className="text-muted">Please wait while we retrieve your payment information.</p>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    );
  }

  if (error) {
    return (
      <Container className="mt-5">
        <Row className="justify-content-center">
          <Col md="6">
            <Card className="border-danger">
              <CardBody className="text-center">
                <div className="mb-3">❌</div>
                <h4 className="text-danger">Error</h4>
                <Alert color="danger" fade={false}>
                  {error}
                </Alert>
                <Button color="primary" onClick={handleReturnHome}>
                  Return to Home
                </Button>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    );
  }

  const currentStatus = paymentStatus?.status || status || 'unknown';
  const isSuccess = currentStatus.toLowerCase() === 'completed' || currentStatus.toLowerCase() === 'success';
  const isFailed = currentStatus.toLowerCase() === 'failed' || currentStatus.toLowerCase() === 'error';
  const isPending = currentStatus.toLowerCase() === 'pending';

  return (
    <Container className="mt-5">
      <Row className="justify-content-center">
        <Col md="8">
          <Card className={`border-${getStatusColor(currentStatus)}`}>
            <CardBody className="text-center">
              <div className="mb-3" style={{ fontSize: '4rem' }}>
                {getStatusIcon(currentStatus)}
              </div>
              
              <h2 className={`text-${getStatusColor(currentStatus)} mb-3`}>
                {isSuccess && 'Payment Successful!'}
                {isFailed && 'Payment Failed'}
                {isPending && 'Payment Pending'}
                {!isSuccess && !isFailed && !isPending && 'Payment Status'}
              </h2>

              <p className="text-muted mb-4">
                {paymentStatus?.message || getStatusMessage(currentStatus)}
              </p>

              {transactionDetails && (
                <div className="mb-4">
                  <Card className="bg-light">
                    <CardBody>
                      <h6>📋 Transaction Details</h6>
                      <Row>
                        <Col md="6">
                          <strong>Transaction ID:</strong><br/>
                          <small className="text-muted">
                            {transactionDetails.pspTransactionId || transactionDetails.transactionId || 'N/A'}
                          </small>
                        </Col>
                        <Col md="6">
                          <strong>Amount:</strong><br/>
                          <small className="text-muted">
                            {transactionDetails.amount || 'N/A'} {transactionDetails.currency || 'RSD'}
                          </small>
                        </Col>
                      </Row>
                      {transactionDetails.description && (
                        <Row className="mt-2">
                          <Col md="12">
                            <strong>Description:</strong><br/>
                            <small className="text-muted">{transactionDetails.description}</small>
                          </Col>
                        </Row>
                      )}
                      {transactionDetails.createdAt && (
                        <Row className="mt-2">
                          <Col md="12">
                            <strong>Date:</strong><br/>
                            <small className="text-muted">
                              {new Date(transactionDetails.createdAt).toLocaleString()}
                            </small>
                          </Col>
                        </Row>
                      )}
                    </CardBody>
                  </Card>
                </div>
              )}

              <div className="d-flex justify-content-center gap-2 flex-wrap">
                {isSuccess && (
                  <Button color="success" size="lg" onClick={handleReturnHome}>
                    🏠 Return to Home
                  </Button>
                )}
                
                {isFailed && (
                  <>
                    <Button color="primary" size="lg" onClick={handleRetryPayment}>
                      🔄 Try Again
                    </Button>
                    <Button color="secondary" size="lg" onClick={handleReturnHome}>
                      🏠 Return to Home
                    </Button>
                  </>
                )}
                
                {isPending && (
                  <Button color="warning" size="lg" onClick={() => window.location.reload()}>
                    🔄 Refresh Status
                  </Button>
                )}
                
                {!isSuccess && !isFailed && !isPending && (
                  <Button color="primary" size="lg" onClick={handleReturnHome}>
                    🏠 Return to Home
                  </Button>
                )}
              </div>

              {isSuccess && (
                <div className="mt-4">
                  <Alert color="success" fade={false}>
                    <strong>🎉 Congratulations!</strong> Your subscription has been activated. 
                    You can now access all the features of your selected package.
                  </Alert>
                </div>
              )}

              {isFailed && (
                <div className="mt-4">
                  <Alert color="danger" fade={false}>
                    <strong>⚠️ Payment Failed</strong> If you continue to experience issues, 
                    please contact our customer support team.
                  </Alert>
                </div>
              )}

              {isPending && (
                <div className="mt-4">
                  <Alert color="warning" fade={false}>
                    <strong>⏳ Processing</strong> Your payment is being processed. 
                    This may take a few minutes. You can refresh this page to check the status.
                  </Alert>
                </div>
              )}
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default PaymentStatusPage;
