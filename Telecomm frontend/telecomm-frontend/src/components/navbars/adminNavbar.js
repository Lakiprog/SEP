import React from "react";
import { Nav, NavLink, Navbar } from "reactstrap";

const AdminNavbar = (props) => {
  return (
    <Navbar color="dark" light expand="md" style={{ fontSize: "25px" }}>
      <Nav>
        <NavLink href="/paymentTypes">Payment types</NavLink>
        <NavLink href="/packageDealsAdmin">Package Deals</NavLink>
        <NavLink href="/login">Logout</NavLink>
      </Nav>
    </Navbar>
  );
};

export default AdminNavbar;
