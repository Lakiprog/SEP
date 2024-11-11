import React from "react";
import HomeNavbar from "../navbars/homeNavbar";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label } from "reactstrap";
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
    <div>
      <HomeNavbar />
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Login</CardTitle>
        <CardBody>
          <Form onSubmit={handleSubmit(login)}>
            <FormGroup>
              <Label>Username</Label>
              <FormInput type="text" name="Username" invalid={!!errors.Username} register={register} />
              {errors.Username && <FormFeedback>{errors.Username.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>Password</Label>
              <FormInput type="password" name="Password" invalid={!!errors.Password} register={register} />
              {errors.Password && <FormFeedback>{errors.Password.message}</FormFeedback>}
            </FormGroup>
            <Button color="primary" type="submit">
              Login
            </Button>
          </Form>
        </CardBody>
      </Card>
    </div>
  );
};

export default Login;
