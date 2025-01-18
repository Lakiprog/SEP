import React from "react";
import { Nav, NavItem, NavLink, Navbar } from "reactstrap";

const PaymentNavbar = (props) => {
  return (
    <Navbar color="dark" expand="md" style={{ fontSize: "25px" }}>
      <Nav>
        {props.paymentTypes?.map((paymentType) => (
          <NavItem key={paymentType.id}>
            <NavLink href="#" onClick={() => props.onSelected(paymentType)}>
              {paymentType.name}
            </NavLink>
          </NavItem>
        ))}
        <NavItem>
          <NavLink href="#" onClick={props.onCancel}>Cancel</NavLink>
        </NavItem>
      </Nav>
    </Navbar>
  );
};

export default PaymentNavbar;

