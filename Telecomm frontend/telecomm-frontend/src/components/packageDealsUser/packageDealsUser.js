import React, { useState } from "react";
import { Card, CardBody, CardTitle, ListGroup, ListGroupItem } from "reactstrap";
import UserNavbar from "../navbars/userNavbar";
import PaymentNavbar from "../navbars/paymentNavbar";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.min.css";
import { useNavigate } from "react-router-dom";
import PaymentContainer from "../paymentContainer/paymentContainer";

const PackageDealsUser = (props) => {
  const CreditCard = 1;
  const QR = 2;
  const Bitcoin = 3;
  const Paypal = 4;

  const [packageDeals, setPackageDeals] = useState([
    { Id: 1, Name: "SBB EON", Price: 100.0 },
    { Id: 2, Name: "SBB EON + Telephone", Price: 110.0 },
    { Id: 3, Name: "SBB Telephone", Price: 10.0 },
  ]);
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [selectedPaymentType, setSelectedPaymentType] = useState(null);
  const [paymentTypes, setPaymentTypes] = useState([
    { Id: 1, Name: "Credit Card" },
    { Id: 2, Name: "QR" },
    { Id: 3, Name: "Bitcoin" },
    { Id: 4, Name: "Paypal" },
  ]);

  const getPackages = () => {
    //TODO
  };

  const getPaymentTypes = () => {
    //TODO
  };

  const onPayed = () => {
    //TODO
  };

  const onPaymentSelected = (paymentType) => {
    switch (paymentType.Id) {
      case CreditCard:
        setSelectedPaymentType(paymentType);
        break;
      case QR:
        toast.success("Successful payment");
        break;
      case Bitcoin:
        toast.success("Successful payment");
        break;
      case Paypal:
        toast.success("Successful payment");
        break;
      default:
        break;
    }
  };

  const choosePackage = (data) => {
    setSelectedPackage(data);
  };

  return (
    <div>
      <ToastContainer />
      {!selectedPackage ? (
        <>
          <UserNavbar />
          <Card className="registration-form" style={{ backgroundColor: "#DEEDE6", borderColor: "black" }}>
            <CardTitle>Package Deals</CardTitle>
            <CardBody>
              <ListGroup>
                {packageDeals.map((packageDeal) => (
                  <ListGroupItem key={packageDeal.Id} action tag="button" onClick={() => choosePackage(packageDeal)}>
                    {`${packageDeal.Name} = ${packageDeal.Price} Euros`}
                  </ListGroupItem>
                ))}
              </ListGroup>
            </CardBody>
          </Card>
        </>
      ) : !selectedPaymentType ? (
        <PaymentNavbar paymentTypes={paymentTypes} onSelected={onPaymentSelected} onCancel={() => setSelectedPackage(null)} />
      ) : (
        <PaymentContainer onPay={onPayed} paymentType={selectedPaymentType} />
      )}
    </div>
  );
};

export default PackageDealsUser;
