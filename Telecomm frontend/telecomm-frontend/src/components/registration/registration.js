import React from "react";
import HomeNavbar from "../navbars/homeNavbar";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { RegistrationValidation } from "./registrationValidation";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label } from "reactstrap";
import FormInput from "../common/formInput";

const Registration = (props) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: yupResolver(RegistrationValidation),
    mode: "onChange",
  });

  const registration = (data) => {
    console.log(data);
  };

  return (
    <div>
      <HomeNavbar />
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Registration</CardTitle>
        <CardBody>
          <Form onSubmit={handleSubmit(registration)}>
            <FormGroup>
              <Label>Username</Label>
              <FormInput type="text" name="Username" invalid={!!errors.Username} register={register} />
              {errors.Username && <FormFeedback>{errors.Username.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>Email</Label>
              <FormInput type="text" name="Email" invalid={!!errors.Email} register={register} />
              {errors.Email && <FormFeedback>{errors.Email.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>Password</Label>
              <FormInput type="password" name="Password" invalid={!!errors.Password} register={register} />
              {errors.Password && <FormFeedback>{errors.Password.message}</FormFeedback>}
            </FormGroup>
            <Button color="primary" type="submit">
              Register
            </Button>
          </Form>
        </CardBody>
      </Card>
    </div>
  );
};

export default Registration;
