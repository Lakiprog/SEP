import React from "react";
import { Nav, NavItem, NavLink, Navbar } from "reactstrap";

const PaymentNavbar = (props) => {
  return (
    <Navbar color="dark" light expand="md" style={{ fontSize: "25px" }}>
      <Nav>
        {props.paymentTypes?.map((paymentType) => (
          <NavItem>
            <NavLink key={paymentType.Id} onClick={() => props.onSelected(paymentType)}>
              {paymentType.Name}
            </NavLink>
          </NavItem>
        ))}
        <NavItem>
          <NavLink onClick={props.onCancel}>Cancel</NavLink>
        </NavItem>
      </Nav>
    </Navbar>
  );
};

export default PaymentNavbar;
