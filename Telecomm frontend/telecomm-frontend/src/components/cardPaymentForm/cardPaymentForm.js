import React from "react";
import { Button, Card, CardBody, CardTitle, Form, FormFeedback, FormGroup, Label } from "reactstrap";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { CardPaymentFormValidation } from "./cardPaymentFormValidation";
import FormInput from "../common/formInput";

const CardPaymentForm = (props) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: yupResolver(CardPaymentFormValidation),
    mode: "onChange",
  });

  const pay = (data) => {};

  return (
    <div>
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Credit Card Details</CardTitle>
        <CardBody>
          <Form onSubmit={handleSubmit(pay)}>
            <FormGroup>
              <Label>Card Number</Label>
              <FormInput type="text" name="CardNumber" invalid={!!errors.CardNumber} placeholder="XXXXXXXXXXXXXXXX" register={register} />
              {errors.CardNumber && <FormFeedback>{errors.CardNumber.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>Card Holder Name</Label>
              <FormInput type="text" name="CardHolderName" invalid={!!errors.CardHolderName} placeholder="John Doe" register={register} />
              {errors.CardHolderName && <FormFeedback>{errors.CardHolderName.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>ExpiryDate</Label>
              <FormInput type="text" name="ExpiryDate" invalid={!!errors.ExpiryDate} placeholder="MM/YY" register={register} />
              {errors.ExpiryDate && <FormFeedback>{errors.ExpiryDate.message}</FormFeedback>}
            </FormGroup>
            <FormGroup>
              <Label>CVC</Label>
              <FormInput type="text" name="CVC" invalid={!!errors.CVC} placeholder="XXX" register={register} />
              {errors.CVC && <FormFeedback>{errors.CVC.message}</FormFeedback>}
            </FormGroup>
            <Button color="primary" type="submit">
              Pay
            </Button>
          </Form>
        </CardBody>
      </Card>
    </div>
  );
};

export default CardPaymentForm;
