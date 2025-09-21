import React, { useEffect, useState } from "react";
import { Button, Card, CardBody, CardTitle, ListGroup, ListGroupItem, Container, Row, Col, Alert } from "reactstrap";
import httpRequest from "../common/httpRequest";
import * as constants from "../common/constants";

const PaymentTypes = (props) => {
  const [clientPaymentTypes, setClientPaymentTypes] = useState([]);
  const [missingPaymentTypes, setMissingPaymentTypes] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPaymentTypes();
  }, []);

  const fetchPaymentTypes = async () => {
    try {
      setLoading(true);
      
      // Get payment methods from PSP for Telecom client (ID: 1)
      const pspPaymentMethods = await httpRequest.get(`https://localhost:7005/api/psp/merchants/1/payment-methods`, {});
      setClientPaymentTypes(pspPaymentMethods || []);

      // Get all available payment methods from PSP admin
      const allPaymentMethods = await httpRequest.get(`https://localhost:7005/api/admin/payment-methods`, {});
      
      // Find missing payment methods (those not assigned to Telecom)
      const missing = allPaymentMethods?.filter((pspType) => 
        pspType.isEnabled && !pspPaymentMethods?.find((clientType) => clientType.id === pspType.id)
      );
      setMissingPaymentTypes(missing || []);
    } catch (err) {
      console.log(err);
    } finally {
      setLoading(false);
    }
  };

  const onDelete = async (data) => {
    try {
      // Remove payment method from Telecom client in PSP
      await httpRequest.delete(`https://localhost:7005/api/admin/merchants/1/payment-methods/${data.id}`, {});
      setClientPaymentTypes(clientPaymentTypes.filter((type) => type.id !== data.id));
      setMissingPaymentTypes([...missingPaymentTypes, { id: data.id, name: data.name }]);
    } catch (err) {
      console.log(err);
    }
  };

  const onAdd = async (data) => {
    try {
      // Add payment method to Telecom client in PSP
      const postData = {
        paymentTypeId: data.id
      };
      await httpRequest.post(`https://localhost:7005/api/admin/merchants/1/payment-methods`, postData);
      
      // Add to local state
      setClientPaymentTypes([...clientPaymentTypes, data]);
      setMissingPaymentTypes(missingPaymentTypes.filter((type) => type.id !== data.id));
    } catch (err) {
      console.log(err);
    }
  };

  const getPaymentIcon = (paymentType) => {
    const name = paymentType.name?.toLowerCase() || paymentType.Name?.toLowerCase();
    switch (name) {
      case 'card':
      case 'credit card':
        return '💳';
      case 'qr':
      case 'qr code':
        return '📱';
      case 'paypal':
        return '🌐';
      case 'bitcoin':
        return '₿';
      default:
        return '💳';
    }
  };

  if (loading) {
    return (
      <Container className="mt-5">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
          <p className="mt-2">Loading payment types...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container className="mt-4">
      <div className="text-center mb-5">
        <h1 className="display-4 text-primary">
          💳 Payment Types Management
        </h1>
        <p className="lead text-muted">
          Manage your available payment methods
        </p>
      </div>

      <Row className="g-4">
        <Col md={6}>
          <Card className="h-100 border-success">
            <CardBody>
              <CardTitle className="text-success">
                <span className="me-2">✅</span>
                Active Payment Types
              </CardTitle>
              {clientPaymentTypes?.length === 0 ? (
                <Alert color="info">
                  No payment types configured yet. Add some from the available options.
                </Alert>
              ) : (
                <ListGroup>
                  {clientPaymentTypes?.map((paymentType) => (
                    <ListGroupItem key={paymentType.id} className="d-flex justify-content-between align-items-center">
                      <div>
                        <span className="me-2">{getPaymentIcon(paymentType)}</span>
                        <strong>{paymentType.name}</strong>
                      </div>
                      <Button 
                        color="danger" 
                        size="sm"
                        onClick={() => onDelete(paymentType)}
                        title="Remove payment type"
                      >
                        🗑️ Remove
                      </Button>
                    </ListGroupItem>
                  ))}
                </ListGroup>
              )}
            </CardBody>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-primary">
            <CardBody>
              <CardTitle className="text-primary">
                <span className="me-2">➕</span>
                Available Payment Types
              </CardTitle>
              {missingPaymentTypes?.length === 0 ? (
                <Alert color="success">
                  All available payment types are already configured!
                </Alert>
              ) : (
                <ListGroup>
                  {missingPaymentTypes?.map((paymentType) => (
                    <ListGroupItem key={paymentType.id} className="d-flex justify-content-between align-items-center">
                      <div>
                        <span className="me-2">{getPaymentIcon(paymentType)}</span>
                        <strong>{paymentType.name}</strong>
                      </div>
                      <Button 
                        color="primary" 
                        size="sm"
                        onClick={() => onAdd(paymentType)}
                        title="Add payment type"
                      >
                        ➕ Add
                      </Button>
                    </ListGroupItem>
                  ))}
                </ListGroup>
              )}
            </CardBody>
          </Card>
        </Col>
      </Row>

      <Row className="mt-4">
        <Col>
          <Card className="border-info">
            <CardBody>
              <h5 className="text-info">
                <span className="me-2">ℹ️</span>
                Payment Types Information
              </h5>
              <p className="mb-2">
                <strong>Active Payment Types:</strong> These are the payment methods currently available to your customers.
              </p>
              <p className="mb-2">
                <strong>Available Payment Types:</strong> These are additional payment methods you can enable for your customers.
              </p>
              <p className="mb-0">
                <strong>Note:</strong> At least one payment type must remain active at all times.
              </p>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default PaymentTypes;
