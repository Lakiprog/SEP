import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, CardText, Button, Modal, ModalHeader, ModalBody, ModalFooter, Form, FormGroup, Label, Input, Badge, TabContent, TabPane, Nav, NavItem, NavLink, Alert, Spinner } from 'reactstrap';
import { toast } from 'react-toastify';
import PaymentTypeSelector from '../paymentTypes/PaymentTypeSelector';
import PaymentContainer from '../paymentContainer/paymentContainer';
import axios from 'axios';

const PackageDealsUser = () => {
  const [packages, setPackages] = useState([]);
  const [subscriptions, setSubscriptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState('1');
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [showSubscriptionModal, setShowSubscriptionModal] = useState(false);
  const [showPaymentForm, setShowPaymentForm] = useState(false);
  const [selectedPaymentType, setSelectedPaymentType] = useState(null);
  const [years, setYears] = useState(1);
  const [subscriptionLoading, setSubscriptionLoading] = useState(false);

  useEffect(() => {
    fetchPackages();
    fetchSubscriptions();
  }, []);

  const fetchPackages = async () => {
    try {
      const response = await axios.get('https://localhost:7010/api/packagedeal/packages');
      setPackages(response.data);
    } catch (err) {
      console.error('Error fetching packages:', err);
      setError('Failed to load packages');
    } finally {
      setLoading(false);
    }
  };

  const fetchSubscriptions = async () => {
    try {
      const userId = 1; // Mock user ID - in real app this would come from auth context
      const response = await axios.get(`https://localhost:7010/api/packagedeal/subscriptions/${userId}`);
      setSubscriptions(response.data);
    } catch (err) {
      console.error('Error fetching subscriptions:', err);
      // Don't set error for subscriptions as it's not critical
    }
  };

  const handleSubscribe = (pkg) => {
    setSelectedPackage(pkg);
    setShowSubscriptionModal(true);
    setShowPaymentForm(false);
    setSelectedPaymentType(null);
  };

  const handlePaymentTypeSelected = (paymentType) => {
    setSelectedPaymentType(paymentType);
    setShowPaymentForm(true);
  };

  const handlePaymentComplete = async (paymentResult) => {
    try {
      console.log('Payment completed:', paymentResult);
      setSubscriptionLoading(true);
      
      // Create subscription
      const subscriptionData = {
        userId: 1, // Mock user ID
        packageId: selectedPackage.id,
        years: years,
        paymentMethod: paymentResult.paymentType,
        subscriptionDate: new Date().toISOString()
      };

      const response = await axios.post('https://localhost:7010/api/packagedeal/subscribe', subscriptionData);
      
      if (response.data) {
        toast.success('Subscription created successfully!');
        
        // Close modal and reset state only after successful subscription creation
        setShowSubscriptionModal(false);
        setShowPaymentForm(false);
        setSelectedPaymentType(null);
        setSelectedPackage(null);
        
        // Refresh subscriptions
        fetchSubscriptions();
      }
    } catch (err) {
      console.error('Error creating subscription:', err);
      toast.error('Failed to create subscription. Please try again.');
    } finally {
      setSubscriptionLoading(false);
    }
  };

  const handleCancelPayment = () => {
    setShowPaymentForm(false);
    setSelectedPaymentType(null);
  };

  const closeModal = () => {
    setShowSubscriptionModal(false);
    setShowPaymentForm(false);
    setSelectedPaymentType(null);
    setSelectedPackage(null);
    setYears(1);
  };

  if (loading) {
    return (
      <Container className="mt-4 text-center">
        <Spinner color="primary" />
        <p className="mt-2">Loading packages...</p>
      </Container>
    );
  }

  if (error) {
    return (
      <Container className="mt-4">
        <Alert color="danger" fade={false}>
          <strong>Error:</strong> {error}
        </Alert>
      </Container>
    );
  }

  return (
    <Container className="mt-4">
      <h2 className="mb-4">📦 Package Deals</h2>
      
      <Nav tabs>
        <NavItem>
          <NavLink
            className={activeTab === '1' ? 'active' : ''}
            onClick={() => setActiveTab('1')}
          >
            📋 Available Packages
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink
            className={activeTab === '2' ? 'active' : ''}
            onClick={() => setActiveTab('2')}
          >
            🔗 My Subscriptions
          </NavLink>
        </NavItem>
      </Nav>

      <TabContent activeTab={activeTab}>
        <TabPane tabId="1">
          <Row>
            {packages.map((pkg) => (
              <Col key={pkg.id} md={6} lg={4} className="mb-4">
                <Card className="h-100 shadow-sm">
                  <CardBody className="d-flex flex-column">
                    <CardTitle className="h5 mb-3">
                      <span className="me-2">📦</span>
                      {pkg.name}
                    </CardTitle>
                    <CardText className="flex-grow-1">
                      {pkg.description}
                    </CardText>
                    <div className="mt-auto">
                      <div className="d-flex justify-content-between align-items-center mb-3">
                        <Badge color="primary" className="fs-6">
                          {pkg.category?.name || 'General'}
                        </Badge>
                        <span className="h5 text-primary mb-0">
                          ${pkg.price}/year
                        </span>
                      </div>
                      <Button
                        color="success"
                        className="w-100"
                        onClick={() => handleSubscribe(pkg)}
                      >
                        Subscribe Now
                      </Button>
                    </div>
                  </CardBody>
                </Card>
              </Col>
            ))}
          </Row>
        </TabPane>

        <TabPane tabId="2">
          {subscriptions.length === 0 ? (
            <Alert color="info" fade={false}>
              <strong>No subscriptions yet!</strong>
              <br />
              Browse available packages and subscribe to get started.
            </Alert>
          ) : (
            <Row>
              {subscriptions.map((subscription) => (
                <Col key={subscription.id} md={6} lg={4} className="mb-4">
                  <Card className="h-100 border-success">
                    <CardBody>
                      <CardTitle className="h6 text-success">
                        <span className="me-2">✅</span>
                        {subscription.package?.name}
                      </CardTitle>
                      <CardText>
                        <strong>Status:</strong> {subscription.status}
                        <br />
                        <strong>Start Date:</strong> {new Date(subscription.startDate).toLocaleDateString()}
                        <br />
                        <strong>End Date:</strong> {new Date(subscription.endDate).toLocaleDateString()}
                        <br />
                        <strong>Payment Method:</strong> {subscription.paymentMethod}
                        <br />
                        <strong>Amount:</strong> ${subscription.amount}
                      </CardText>
                    </CardBody>
                  </Card>
                </Col>
              ))}
            </Row>
          )}
        </TabPane>
      </TabContent>

      {/* Subscription Modal */}
      <Modal isOpen={showSubscriptionModal} toggle={closeModal} size="lg">
        <ModalHeader toggle={closeModal}>
          <span className="me-2">📦</span>
          Subscribe to {selectedPackage?.name}
        </ModalHeader>
        <ModalBody>
          {!showPaymentForm ? (
            <div>
              <div className="mb-4 p-3 bg-light rounded">
                <h6>📋 Package Details</h6>
                <Row>
                  <Col md="6">
                    <strong>Package:</strong> {selectedPackage?.name}
                  </Col>
                  <Col md="6">
                    <strong>Category:</strong> {selectedPackage?.category?.name}
                  </Col>
                </Row>
                <Row className="mt-2">
                  <Col md="6">
                    <strong>Price per year:</strong> ${selectedPackage?.price}
                  </Col>
                  <Col md="6">
                    <strong>Description:</strong> {selectedPackage?.description}
                  </Col>
                </Row>
              </div>

              <FormGroup>
                <Label for="years">Subscription Duration (Years)</Label>
                <Input
                  id="years"
                  type="select"
                  value={years}
                  onChange={(e) => setYears(parseInt(e.target.value))}
                >
                  <option value={1}>1 Year</option>
                  <option value={2}>2 Years</option>
                  <option value={3}>3 Years</option>
                  <option value={5}>5 Years</option>
                </Input>
              </FormGroup>

              <div className="mb-3 p-3 bg-primary text-white rounded">
                <h6 className="mb-2">💰 Total Cost</h6>
                <h4 className="mb-0">${selectedPackage ? selectedPackage.price * years : 0}</h4>
                <small>${selectedPackage?.price} × {years} year(s)</small>
              </div>

              <PaymentTypeSelector
                onPaymentSelected={handlePaymentTypeSelected}
                availablePaymentTypes={[
                  { id: 'card', name: 'Credit Card', icon: '💳', color: 'primary' },
                  { id: 'qr', name: 'QR Code', icon: '📱', color: 'success' },
                  { id: 'paypal', name: 'PayPal', icon: '🌐', color: 'info' },
                  { id: 'bitcoin', name: 'Bitcoin', icon: '₿', color: 'warning' }
                ]}
              />
            </div>
          ) : (
            <PaymentContainer
              selectedPaymentType={selectedPaymentType}
              selectedPackage={selectedPackage}
              years={years}
              onPaymentComplete={handlePaymentComplete}
              onCancel={handleCancelPayment}
            />
          )}
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={closeModal}>
            Cancel
          </Button>
        </ModalFooter>
      </Modal>
    </Container>
  );
};

export default PackageDealsUser;
