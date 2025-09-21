import React, { useState } from "react";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label, Container, Row, Col, Alert } from "reactstrap";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { LoginValidation } from "./loginValidation";
import { useNavigate } from "react-router-dom";
import FormInput from "../common/formInput";
import { TELECOM_API_BASE_URL } from "../common/constants";
import httpRequest from "../common/httpRequest";
import { useAuth } from "../../contexts/AuthContext";

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
  const { login: authLogin } = useAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const login = async (data) => {
    setLoading(true);
    setError("");

    try {
      const loginData = {
        Username: data.Username,
        Password: data.Password
      };

      const response = await httpRequest.post(`${TELECOM_API_BASE_URL}/User/login`, loginData);

      // Store authentication data
      authLogin(response);

      // Based on user type, navigate to appropriate page
      if (response.userType === "SuperAdmin") {
        navigate("/packageDealsAdmin");
      } else {
        navigate("/packageDealsUser");
      }
    } catch (err) {
      console.error('Login error:', err);

      let errorMessage = 'Login failed. Please try again.';

      if (err.status === 401) {
        errorMessage = 'Invalid username or password. Please check your credentials and try again.';
      } else if (err.status === 400) {
        errorMessage = err.errMsg || 'Invalid request. Please check your input.';
      } else if (err.status >= 500) {
        errorMessage = 'Server error. Please try again later.';
      } else if (err.errMsg) {
        // Use server error message if available
        errorMessage = err.errMsg;
      }

      setError(errorMessage);
    } finally {
      setLoading(false);
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

                {error && (
                  <Alert color="danger" className="mb-3">
                    {error}
                  </Alert>
                )}

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
                  
                  <Button color="primary" size="lg" type="submit" className="w-100 mb-3" disabled={loading}>
                    {loading ? '⏳ Logging in...' : '🔑 Login'}
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
              <p className="text-muted">
                Use your registered credentials to login to your telecommunications account.
              </p>
            </div>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default Login;
