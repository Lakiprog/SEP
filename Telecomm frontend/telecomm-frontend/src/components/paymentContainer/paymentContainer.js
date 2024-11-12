import React from "react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.min.css";
import CardPaymentForm from "../cardPaymentForm/cardPaymentForm";

const PaymentContainer = (props) => {
  const CreditCard = 1;
  const QR = 2;
  const Bitcoin = 3;
  const Paypal = 4;

  const paymentForm = () => {
    switch (props.paymentType.Id) {
      case CreditCard:
        return <CardPaymentForm onPay={props.onPay} paymentType={props.paymentType} />;
      case QR:
        return <></>;
      case Bitcoin:
        return <></>;
      case Paypal:
        return <></>;
      default:
        return <></>;
    }
  };

  return (
    <div>
      <ToastContainer />
      {paymentForm()}
    </div>
  );
};

export default PaymentContainer;
