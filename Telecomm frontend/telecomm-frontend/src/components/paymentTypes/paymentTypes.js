import React, { useEffect, useState } from "react";
import axios from "axios";
import { Button, Card, CardBody, CardTitle, ListGroup, ListGroupItem } from "reactstrap";
import AdminNavbar from "../navbars/adminNavbar";

const PaymentTypes = (props) => {
  const [clientPaymentTypes, setClientPaymentTypes] = useState([
    { Id: 1, Name: "Credit Card" },
    { Id: 2, Name: "QR" },
  ]);
  const [missingPaymentTypes, setMissingPaymentTypes] = useState([
    { Id: 3, Name: "Bitcoin" },
    { Id: 4, Name: "Paypal" },
  ]);

  useEffect(() => {
    axios
      .get("https://localhost:7115/api/payment-types", {
        // withCredentials: true,
        // credentials: "include", // "same-origin",
        mode: "no-cors",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        cache: false,
      })
      .then((resp) => {
        console.log(resp);
      });
  }, []);

  const onDelete = () => {};

  const onAdd = () => {};

  return (
    <div>
      <AdminNavbar />
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Your Payment Types</CardTitle>
        <CardBody>
          <ListGroup>
            {clientPaymentTypes.map((paymentType) => (
              <ListGroupItem key={paymentType.Id} action tag="button">
                <Button color="danger" style={{ marginBottom: "5px", marginLeft: "10px", marginRight: "10px" }} onClick={() => onDelete(paymentType)}>
                  Remove
                </Button>
                {paymentType.Name}
              </ListGroupItem>
            ))}
          </ListGroup>
        </CardBody>
      </Card>
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Missing Payment Types</CardTitle>
        <CardBody>
          <ListGroup>
            {missingPaymentTypes.map((paymentType) => (
              <ListGroupItem key={paymentType.Id} action tag="button">
                <Button color="primary" style={{ marginBottom: "5px", marginLeft: "10px", marginRight: "10px" }} onClick={() => onAdd(paymentType)}>
                  Add
                </Button>
                {paymentType.Name}
              </ListGroupItem>
            ))}
          </ListGroup>
        </CardBody>
      </Card>
    </div>
  );
};

export default PaymentTypes;
