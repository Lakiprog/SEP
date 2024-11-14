import React, { useEffect, useState } from "react";
import { Button, Card, CardBody, CardTitle, ListGroup, ListGroupItem } from "reactstrap";
import AdminNavbar from "../navbars/adminNavbar";
import httpRequest from "../common/httpRequest";
import * as constants from "../common/constants";

const PaymentTypes = (props) => {
  const [clientPaymentTypes, setClientPaymentTypes] = useState([]);
  const [missingPaymentTypes, setMissingPaymentTypes] = useState([]);

  useEffect(() => {
    httpRequest
      .get(`https://localhost:${constants.PORT}/api/payment-types/GetAllPayments`, { clientId: "1" })
      .then((resp) => {
        setClientPaymentTypes(resp);

        httpRequest.get(`https://localhost:${constants.PORT}/api/payment-types`, {}).then((resp2) => {
          setMissingPaymentTypes(resp2?.filter((pspType) => resp?.findIndex((clientType) => clientType.id === pspType.id) === -1));
        });
      })
      .catch((err) => console.log(err));
  }, []);

  const onDelete = (data) => {
    httpRequest
      .delete(`https://localhost:${constants.PORT}/api/payment-types`, { id: data.id })
      .then((resp) => {
        setClientPaymentTypes(clientPaymentTypes.filter((type) => type.id !== data.id));
        setMissingPaymentTypes([...missingPaymentTypes, { id: data.PaymentTypeId, Name: data.Name }]);
      })
      .catch((err) => console.log(err));
  };

  const onAdd = (data) => {
    const postData = {
      paymentTypeId: data.id,
      clientId: "1",
      name: data.name,
    };
    httpRequest.post(`https://localhost:${constants.PORT}/api/payment-types`, postData).then((resp) => {
      setClientPaymentTypes([...clientPaymentTypes, resp]);
      setMissingPaymentTypes(missingPaymentTypes.filter((type) => type.id !== data.id));
    });
  };

  return (
    <div>
      <AdminNavbar />
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Your Payment Types</CardTitle>
        <CardBody>
          <ListGroup>
            {clientPaymentTypes?.map((paymentType) => (
              <ListGroupItem key={paymentType.id} action>
                <Button color="danger" style={{ marginBottom: "5px", marginLeft: "10px", marginRight: "10px" }} onClick={() => onDelete(paymentType)}>
                  Remove
                </Button>
                {paymentType.name}
              </ListGroupItem>
            ))}
          </ListGroup>
        </CardBody>
      </Card>
      <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
        <CardTitle>Missing Payment Types</CardTitle>
        <CardBody>
          <ListGroup>
            {missingPaymentTypes?.map((paymentType) => (
              <ListGroupItem key={paymentType.id} action>
                <Button color="primary" style={{ marginBottom: "5px", marginLeft: "10px", marginRight: "10px" }} onClick={() => onAdd(paymentType)}>
                  Add
                </Button>
                {paymentType.name}
              </ListGroupItem>
            ))}
          </ListGroup>
        </CardBody>
      </Card>
    </div>
  );
};

export default PaymentTypes;
