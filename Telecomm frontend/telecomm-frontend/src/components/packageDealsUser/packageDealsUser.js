import React, { useEffect, useState } from "react";
import {
  Accordion,
  AccordionBody,
  AccordionHeader,
  AccordionItem,
  Button,
  Card,
  CardBody,
  CardTitle,
  Input,
  Label,
  ListGroup,
  ListGroupItem,
} from "reactstrap";
import UserNavbar from "../navbars/userNavbar";
import PaymentNavbar from "../navbars/paymentNavbar";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.min.css";
import { useNavigate } from "react-router-dom";
import PaymentContainer from "../paymentContainer/paymentContainer";
import httpRequest from "../common/httpRequest";
import * as constants from "../common/constants";

const PackageDealsUser = (props) => {
  const CreditCard = 1;
  const QR = 2;
  const Bitcoin = 3;
  const Paypal = 4;

  const [open, setOpen] = useState("1");
  const [packageDeals, setPackageDeals] = useState([
    { id: 1, name: "SBB EON", description: "OK", price: 100.0 },
    { id: 2, name: "SBB EON + Telephone", description: "skupo", price: 110.0 },
    { id: 3, name: "SBB Telephone", description: "lose lose lose", price: 10.0 },
  ]);
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [selectedPaymentType, setSelectedPaymentType] = useState(null);
  const [paymentTypes, setPaymentTypes] = useState([]);
  const [years, setYears] = useState(1);

  useEffect(() => {
    httpRequest
      .get(`https://localhost:${constants.PORT}/api/payment-types`, { clientId: "1" })
      .then((resp) => {
        setPaymentTypes(resp ?? []);
      })
      .catch((err) => console.log(err));
  }, []);

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
    switch (paymentType.id) {
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

  const toggle = (id) => {
    if (open === id) {
      setOpen();
    } else {
      setOpen(id);
      setYears(1);
    }
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
              <Accordion open={open} toggle={toggle}>
                {packageDeals?.map((packageDeal) => (
                  <AccordionItem key={packageDeal.id}>
                    <AccordionHeader targetId={packageDeal.id}> {packageDeal.name}</AccordionHeader>
                    <AccordionBody accordionId={packageDeal.id}>
                      <div>{packageDeal.description}</div>
                      <div style={{ marginTop: "25px" }}>
                        <b>{`Price per year = ${packageDeal.price}$`}</b>
                      </div>
                      <div style={{ marginTop: "25px" }}>
                        <Button color="primary" onClick={() => choosePackage(packageDeal)}>{`Subscribe for ${years} years for ${
                          years * packageDeal.price
                        }$`}</Button>
                      </div>
                      <hr />
                      <div style={{ marginTop: "20px" }}>
                        <Label>Number of subscription years:</Label>
                        <Input type="number" min="1" value={years} onChange={(e) => setYears(e.target.value)} />
                      </div>
                    </AccordionBody>
                  </AccordionItem>
                ))}
              </Accordion>
            </CardBody>
          </Card>
        </>
      ) : !selectedPaymentType ? (
        <PaymentNavbar paymentTypes={paymentTypes} onSelected={onPaymentSelected} onCancel={() => setSelectedPackage(null)} />
      ) : (
        <PaymentContainer onPay={onPayed} paymentType={selectedPaymentType} package={selectedPackage} years={years} />
      )}
    </div>
  );
};

export default PackageDealsUser;
