import React from "react";
import { Nav, NavLink, Navbar, Container, Row, Col, Card, CardBody, CardTitle, CardText, Button } from "reactstrap";

const HomeNavbar = (props) => {
  return (
    <div>
      <Navbar color="dark" dark expand="md" style={{ fontSize: "20px" }}>
        <Container>
          <Nav className="me-auto">
            <NavLink href="/" className="text-white">
              <strong>Telekom SEP</strong>
            </NavLink>
          </Nav>
          <Nav>
            <NavLink href="/login" className="text-white">Login</NavLink>
            <NavLink href="/registration" className="text-white">Registration</NavLink>
          </Nav>
        </Container>
      </Navbar>

      <Container className="mt-5">
        <Row className="mb-4">
          <Col>
            <h1 className="text-center mb-4">Welcome to Telekom SEP</h1>
            <p className="text-center lead">
              Electronic payment system for telecommunications services
            </p>
          </Col>
        </Row>

        <Row className="g-4">
          <Col md={6} lg={3}>
            <Card className="h-100 text-center">
              <CardBody>
                <div className="text-primary mb-3" style={{ fontSize: "48px", fontWeight: "bold" }}>👥</div>
                <CardTitle tag="h5">Users</CardTitle>
                <CardText>
                  View and manage telecommunications packages
                </CardText>
                <Button color="primary" href="/packageDealsUser">
                  View Packages
                </Button>
              </CardBody>
            </Card>
          </Col>

          <Col md={6} lg={3}>
            <Card className="h-100 text-center">
              <CardBody>
                <div className="text-success mb-3" style={{ fontSize: "48px", fontWeight: "bold" }}>🛒</div>
                <CardTitle tag="h5">Purchase</CardTitle>
                <CardText>
                  Subscribe to telecommunications packages
                </CardText>
                <Button color="success" href="/packageDealsUser">
                  Buy Package
                </Button>
              </CardBody>
            </Card>
          </Col>

          <Col md={6} lg={3}>
            <Card className="h-100 text-center">
              <CardBody>
                <div className="text-warning mb-3" style={{ fontSize: "48px", fontWeight: "bold" }}>⚙️</div>
                <CardTitle tag="h5">Administration</CardTitle>
                <CardText>
                  Manage packages and users
                </CardText>
                <Button color="warning" href="/packageDealsAdmin">
                  Admin Panel
                </Button>
              </CardBody>
            </Card>
          </Col>

          <Col md={6} lg={3}>
            <Card className="h-100 text-center">
              <CardBody>
                <div className="text-info mb-3" style={{ fontSize: "48px", fontWeight: "bold" }}>💳</div>
                <CardTitle tag="h5">Payment</CardTitle>
                <CardText>
                  Multiple payment methods: Card, QR, PayPal, Bitcoin
                </CardText>
                <Button color="info" href="/paymentTypes">
                  Payment Methods
                </Button>
              </CardBody>
            </Card>
          </Col>
        </Row>

        <Row className="mt-5">
          <Col>
            <Card>
              <CardBody>
                <h3 className="text-center mb-4">Supported Payment Methods</h3>
                <Row className="text-center">
                  <Col md={3}>
                    <div className="text-primary mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>💳</div>
                    <p><strong>Credit Card</strong></p>
                    <p className="text-muted">Secure payment through banking system</p>
                  </Col>
                  <Col md={3}>
                    <div className="text-success mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>📱</div>
                    <p><strong>QR Code</strong></p>
                    <p className="text-muted">Fast payment via mobile device</p>
                  </Col>
                  <Col md={3}>
                    <div className="text-info mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>🌐</div>
                    <p><strong>PayPal</strong></p>
                    <p className="text-muted">International payment</p>
                  </Col>
                  <Col md={3}>
                    <div className="text-warning mb-2" style={{ fontSize: "32px", fontWeight: "bold" }}>₿</div>
                    <p><strong>Bitcoin</strong></p>
                    <p className="text-muted">Cryptocurrency payment</p>
                  </Col>
                </Row>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default HomeNavbar;
