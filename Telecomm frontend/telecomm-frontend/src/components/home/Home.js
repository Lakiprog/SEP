import React from "react";
import { Container, Row, Col, Card, CardBody, CardTitle, CardText, Button } from "reactstrap";
import { useAuth } from "../../contexts/AuthContext";

const Home = () => {
  const { user, isAuthenticated } = useAuth();
  const isAdmin = isAuthenticated && user?.userType === "SuperAdmin";

  return (
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

        {isAdmin && (
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
        )}
      </Row>
    </Container>
  );
};

export default Home;