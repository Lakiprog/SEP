import React from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { RegistrationValidation } from "./registrationValidation";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label, Container, Row, Col } from "reactstrap";
import FormInput from "../common/formInput";
import { useNavigate } from "react-router-dom";

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

  const registration = (data) => {
    console.log(data);
    // In real implementation, this would call the backend API
    alert('Registration successful! You can now login.');
    navigate("/login");
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
                  
                  <Button color="primary" size="lg" type="submit" className="w-100 mb-3">
                    📝 Create Account
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
