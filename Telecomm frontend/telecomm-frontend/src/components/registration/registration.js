import React, { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { RegistrationValidation } from "./registrationValidation";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label, Container, Row, Col, Alert } from "reactstrap";
import FormInput from "../common/formInput";
import { useNavigate } from "react-router-dom";
import { TELECOM_API_BASE_URL } from "../common/constants";
import httpRequest from "../common/httpRequest";

const Registration = (props) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: yupResolver(RegistrationValidation),
    mode: "onChange",
  });

  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const registration = async (data) => {
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      const userData = {
        Username: data.Username,
        Email: data.Email,
        Password: data.Password
      };

      const response = await httpRequest.post(`${TELECOM_API_BASE_URL}/User/registerUser`, userData);

      setSuccess('Registration successful! You can now login.');
      setTimeout(() => {
        navigate("/login");
      }, 2000);
    } catch (err) {
      console.error('Registration error:', err);

      let errorMessage = 'Registration failed. Please try again.';

      if (err.status === 400) {
        errorMessage = err.errMsg || 'Invalid registration data. Please check your input.';
      } else if (err.status === 409) {
        errorMessage = 'Username or email already exists. Please choose different credentials.';
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
                📝 Registration
              </h1>
              <p className="lead text-muted">
                Create your telecommunications account
              </p>
            </div>

            <Card className="shadow">
              <CardBody className="p-4">
                <CardTitle className="text-center mb-4">
                  <span className="text-primary">👤</span> New User Registration
                </CardTitle>

                {error && (
                  <Alert color="danger" className="mb-3">
                    {error}
                  </Alert>
                )}

                {success && (
                  <Alert color="success" className="mb-3">
                    {success}
                  </Alert>
                )}

                <Form onSubmit={handleSubmit(registration)}>
                  <FormGroup className="mb-3">
                    <Label for="Username">
                      <span className="me-2">👤</span>Username
                    </Label>
                    <FormInput 
                      type="text" 
                      name="Username" 
                      invalid={!!errors.Username} 
                      register={register} 
                      placeholder="Choose a username"
                    />
                    {errors.Username && <FormFeedback>{errors.Username.message}</FormFeedback>}
                  </FormGroup>
                  
                  <FormGroup className="mb-3">
                    <Label for="Email">
                      <span className="me-2">📧</span>Email
                    </Label>
                    <FormInput 
                      type="email" 
                      name="Email" 
                      invalid={!!errors.Email} 
                      register={register} 
                      placeholder="Enter your email address"
                    />
                    {errors.Email && <FormFeedback>{errors.Email.message}</FormFeedback>}
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
                      placeholder="Create a strong password"
                    />
                    {errors.Password && <FormFeedback>{errors.Password.message}</FormFeedback>}
                  </FormGroup>
                  
                  <Button color="primary" size="lg" type="submit" className="w-100 mb-3" disabled={loading}>
                    {loading ? '⏳ Creating Account...' : '📝 Create Account'}
                  </Button>
                  
                  <div className="text-center">
                    <Button 
                      color="link" 
                      onClick={() => navigate("/login")}
                      className="text-decoration-none"
                    >
                      🔐 Already have an account? Login here
                    </Button>
                  </div>
                </Form>
              </CardBody>
            </Card>

            <div className="text-center mt-4">
              <p className="text-muted">
                By registering, you agree to our terms of service and privacy policy.
              </p>
            </div>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default Registration;
