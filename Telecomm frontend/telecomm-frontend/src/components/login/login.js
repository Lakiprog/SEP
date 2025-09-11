import React from "react";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label, Container, Row, Col, Alert } from "reactstrap";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { LoginValidation } from "./loginValidation";
import { useNavigate } from "react-router-dom";
import FormInput from "../common/formInput";

const Login = (props) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: yupResolver(LoginValidation),
    mode: "onChange",
  });

  const navigate = useNavigate();

  const login = (data) => {
    if (data.Username === "admin") {
      navigate("/paymentTypes");
    } else {
      navigate("/packageDealsUser");
    }
  };

  return (
    <div className="bg-light min-vh-100">
      <Container className="py-5">
        <Row className="justify-content-center">
          <Col md={6} lg={4}>
            <div className="text-center mb-4">
              <h1 className="display-4 text-primary">
                🔐 Login
              </h1>
              <p className="lead text-muted">
                Access your telecommunications account
              </p>
            </div>

            <Card className="shadow">
              <CardBody className="p-4">
                <CardTitle className="text-center mb-4">
                  <span className="text-primary">👤</span> User Authentication
                </CardTitle>
                
                <Form onSubmit={handleSubmit(login)}>
                  <FormGroup className="mb-3">
                    <Label for="Username">
                      <span className="me-2">👤</span>Username
                    </Label>
                    <FormInput 
                      type="text" 
                      name="Username" 
                      invalid={!!errors.Username} 
                      register={register} 
                      placeholder="Enter your username"
                    />
                    {errors.Username && <FormFeedback>{errors.Username.message}</FormFeedback>}
                  </FormGroup>
                  
                  <FormGroup className="mb-4">
                    <Label for="Password">
                      <span className="me-2">🔒</span>Password
                    </Label>
                    <FormInput 
                      type="password" 
                      name="Password" 
                      invalid={!!errors.Password} 
                      register={register} 
                      placeholder="Enter your password"
                    />
                    {errors.Password && <FormFeedback>{errors.Password.message}</FormFeedback>}
                  </FormGroup>
                  
                  <Button color="primary" size="lg" type="submit" className="w-100 mb-3">
                    🔑 Login
                  </Button>
                  
                  <div className="text-center">
                    <Button 
                      color="link" 
                      onClick={() => navigate("/registration")}
                      className="text-decoration-none"
                    >
                      📝 Don't have an account? Register here
                    </Button>
                  </div>
                </Form>
              </CardBody>
            </Card>

            <div className="text-center mt-4">
              <Alert color="info" className="d-inline-block">
                <strong>Demo Accounts:</strong><br/>
                <strong>Admin:</strong> username: "admin", password: any<br/>
                <strong>User:</strong> username: any other, password: any
              </Alert>
            </div>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default Login;
