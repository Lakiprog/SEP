import React, { useState, useEffect } from 'react';
import { Card, CardBody, CardHeader, Button, Alert, Row, Col, Spinner } from 'reactstrap';
import PSPCardPaymentForm from '../pspCardPaymentForm/pspCardPaymentForm';
import axios from 'axios';

const PaymentContainer = ({ selectedPaymentType, selectedPackage, years, onPaymentComplete, onCancel }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [qrCodeData, setQrCodeData] = useState(null);

  // Debug useEffect to see when qrCodeData changes
  useEffect(() => {
    console.log('qrCodeData changed:', qrCodeData);
  }, [qrCodeData]);

  const handleCardPayment = async (transactionId) => {
    setSuccess(true);
    setLoading(false);
    
    // Call parent callback
    if (onPaymentComplete) {
      onPaymentComplete({
        paymentType: 'card',
        transactionId,
        amount: selectedPackage.price * years,
        status: 'success'
      });
    }
  };

  const initiatePayment = async (paymentType) => {
    setLoading(true);
    setError('');

    try {
      const paymentData = {
        subscriptionId: Math.floor(Math.random() * 1000), // Mock subscription ID
        paymentMethod: paymentType,
        amount: selectedPackage.price * years,
        currency: paymentType === 'paypal' ? 'EUR' : 'RSD',
        description: `Subscription to ${selectedPackage.name} for ${years} year(s)`,
        returnUrl: window.location.origin + '/success',
        cancelUrl: window.location.origin + '/cancel'
      };

      // Call Gateway service directly (not through Telecom to avoid circular calls)
      console.log('Sending payment request:', paymentData);
      const response = await axios.post('https://localhost:5001/api/PackageDeal/payment/initiate-psp', paymentData);
      console.log('Payment response:', response.data);
      if (response.data.success) {
        setLoading(false);
        
        // If QR payment, store QR code data and show it
        if (paymentType === 'qr' && response.data.qrCode) {
          console.log('QR code generated successfully!');
          console.log('QR kod data:', response.data.qrCode);
          console.log('QR kod length:', response.data.qrCode.length);
          setQrCodeData({
            qrCode: response.data.qrCode,
            amount: response.data.amount,
            currency: response.data.currency,
            accountNumber: response.data.accountNumber,
            receiverName: response.data.receiverName,
            orderId: response.data.orderId,
            message: response.data.message
          });
          console.log('QR kod data set in state');
          // We don't call onPaymentComplete - user must click "Payment completed"
        } else {
          setSuccess(true);
          // Call parent callback for other payment types
          if (onPaymentComplete) {
            onPaymentComplete({
              paymentType,
              paymentId: response.data.paymentId,
              amount: selectedPackage.price * years,
              status: 'success'
            });
          }
        }
      } else {
        setError(response.data.errorMessage || 'Payment initiation failed');
        setLoading(false);
      }
    } catch (err) {
      console.error('Payment initiation error:', err);
      console.error('Error response:', err.response?.data);
      setError(`Failed to initiate payment: ${err.response?.data?.error || err.message}`);
      setLoading(false);
    }
  };

  if (success) {
    return (
      <Card className="border-success">
        <CardBody className="text-center">
          <div className="mb-3">✅</div>
          <h4 className="text-success">Payment Initiated Successfully!</h4>
          <p className="text-muted">Your payment is being processed.</p>
        </CardBody>
      </Card>
    );
  }

  if (selectedPaymentType?.id === 'card') {
    return (
      <PSPCardPaymentForm
        selectedPackage={selectedPackage}
        years={years}
        onPay={handleCardPayment}
        onCancel={onCancel}
      />
    );
  }

  const renderPaymentMethod = () => {
    switch (selectedPaymentType?.id) {
      case 'qr':
        console.log('Rendering QR payment, qrCodeData:', qrCodeData);
        console.log('QR kod exists:', !!qrCodeData);
        console.log('QR kod qrCode property:', qrCodeData?.qrCode);
        return (
          <div className="text-center">
            {qrCodeData ? (
              <div>
                <h5>QR Code Payment</h5>
                <p className="text-muted mb-3">
                  {qrCodeData.message || "Scan the QR code with your banking app to complete the payment."}
                </p>
                
                <div className="mb-4 text-center">
                  {qrCodeData.qrCode ? (
                    <div>
                      <img 
                        src={`data:image/png;base64,${qrCodeData.qrCode}`} 
                        alt="QR Code for Payment" 
                        className="img-fluid mx-auto d-block"
                        style={{ 
                          maxWidth: '400px', 
                          width: '100%',
                          height: 'auto',
                          border: '2px solid #28a745', 
                          borderRadius: '12px',
                          boxShadow: '0 4px 8px rgba(0,0,0,0.1)',
                          backgroundColor: 'white',
                          padding: '10px'
                        }}
                      />
                      <p className="text-muted mt-2">
                        <strong>📱 Scan the QR code with your banking app</strong>
                      </p>
                    </div>
                  ) : (
                    <div className="alert alert-warning">
                      QR code was not generated.
                    </div>
                  )}
                </div>
                
                <div className="mb-3 p-3 bg-light rounded">
                  <h6>Payment details:</h6>
                  <p className="mb-1"><strong>Amount:</strong> {qrCodeData.amount} {qrCodeData.currency}</p>
                  <p className="mb-1"><strong>Receiver:</strong> {qrCodeData.receiverName}</p>
                  <p className="mb-1"><strong>Account Number:</strong> {qrCodeData.accountNumber}</p>
                  <p className="mb-0"><strong>Order ID:</strong> {qrCodeData.orderId}</p>
                </div>
                
                <div className="mb-3">
                  <div className="alert alert-info">
                    <strong>💡 Instructions:</strong> Scan the QR code with your banking app, then click "Payment completed" when you finish the transaction.
                  </div>
                  
                  <div className="d-grid gap-2 d-md-flex justify-content-md-center">
                    <Button 
                      color="success" 
                      size="lg"
                      onClick={() => {
                        console.log('Payment completed clicked - calling onPaymentComplete');
                        if (onPaymentComplete) {
                          onPaymentComplete({
                            paymentType: 'qr',
                            paymentId: qrCodeData.orderId,
                            amount: qrCodeData.amount,
                            status: 'completed'
                          });
                        }
                      }}
                      className="me-2"
                    >
                      ✅ Payment completed
                    </Button>
                    <Button 
                      color="info" 
                      size="lg"
                      onClick={async () => {
                        try {
                          const response = await axios.post('https://localhost:5001/api/Payment/validate-qr', {
                            QRCodeData: `BCD\n0002\n1\nAIKBRSBG\n${qrCodeData.receiverName}\n${qrCodeData.accountNumber}\n97\n${qrCodeData.orderId}\nAC01\n${qrCodeData.amount}\n${qrCodeData.currency}`
                          });
                          if (response.data.isValid) {
                            alert('✅ QR code is valid according to NBS IPS standard!');
                          } else {
                            alert('❌ QR code is not valid. Errors: ' + response.data.errors.join(', '));
                          }
                        } catch (err) {
                          alert('❌ Error validating QR code');
                        }
                      }}
                      className="me-2"
                    >
                      🔍 Validate QR code
                    </Button>
                    <Button 
                      color="secondary" 
                      size="lg"
                      onClick={() => {
                        setQrCodeData(null);
                        setError('');
                      }}
                    >
                      🔄 Generate new QR code
                    </Button>
                  </div>
                </div>
              </div>
            ) : (
              <div>
                <div className="mb-3">
                  <div className="border p-4 d-inline-block rounded">
                    <div className="text-muted">📱</div>
                    <div className="text-muted">QR Code</div>
                  </div>
                </div>
                <h5>QR Code Payment</h5>
                <p className="text-muted">
                  Generate QR code for payment via mobile banking app.
                </p>
                <div className="mb-3">
                  <strong>Amount:</strong> {selectedPackage.price * years} RSD
                </div>
                <Button
                  color="primary"
                  onClick={() => initiatePayment('qr')}
                  disabled={loading}
                  className="me-2"
                >
                  {loading ? <Spinner size="sm" /> : 'Generate QR code'}
                </Button>
                <Button color="secondary" onClick={onCancel} disabled={loading}>
                  Cancel
                </Button>
              </div>
            )}
          </div>
        );

      case 'bitcoin':
        return (
          <div className="text-center">
            <div className="mb-3">
              <div className="border p-4 d-inline-block rounded">
                <div className="text-muted">₿</div>
                <div className="text-muted">Bitcoin</div>
              </div>
            </div>
            <h5>Bitcoin Payment</h5>
            <p className="text-muted">
              Complete your payment using Bitcoin cryptocurrency.
            </p>
            <div className="mb-3">
              <strong>Amount:</strong> ${selectedPackage.price * years} RSD
            </div>
            <Button
              color="warning"
              onClick={() => initiatePayment('bitcoin')}
              disabled={loading}
              className="me-2"
            >
              {loading ? <Spinner size="sm" /> : 'Pay with Bitcoin'}
            </Button>
            <Button color="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        );

      case 'paypal':
        return (
          <div className="text-center">
            <div className="mb-3">
              <div className="border p-4 d-inline-block rounded">
                <div className="text-muted">🌐</div>
                <div className="text-muted">PayPal</div>
              </div>
            </div>
            <h5>PayPal Payment</h5>
            <p className="text-muted">
              Complete your payment using your PayPal account.
            </p>
            <div className="mb-3">
              <strong>Amount:</strong> €{(selectedPackage.price * years * 0.0085).toFixed(2)} EUR
            </div>
            <Button
              color="info"
              onClick={() => initiatePayment('paypal')}
              disabled={loading}
              className="me-2"
            >
              {loading ? <Spinner size="sm" /> : 'Pay with PayPal'}
            </Button>
            <Button color="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        );

      default:
        return (
          <div className="text-center">
            <p className="text-muted">Please select a payment method.</p>
          </div>
        );
    }
  };

  return (
    <Card>
      <CardHeader className="bg-primary text-white">
        <h4 className="mb-0">
          {selectedPaymentType?.id === 'card' && '💳'}
          {selectedPaymentType?.id === 'qr' && '📱'}
          {selectedPaymentType?.id === 'bitcoin' && '₿'}
          {selectedPaymentType?.id === 'paypal' && '🌐'}
          {' '}
          {selectedPaymentType?.name?.toUpperCase() || selectedPaymentType?.toUpperCase() || 'PAYMENT'} Payment
        </h4>
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
              <strong>Total Amount:</strong> <span className="text-primary fw-bold">${selectedPackage?.price * years}</span>
            </Col>
          </Row>
        </div>

        {renderPaymentMethod()}
      </CardBody>
    </Card>
  );
};

export default PaymentContainer;
