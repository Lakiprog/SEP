import React from "react";
import { Nav, NavLink, Navbar } from "reactstrap";

const UserNavbar = (props) => {
  return (
    <Navbar color="dark" light expand="md" style={{ fontSize: "25px" }}>
      <Nav>
        <NavLink href="/packageDealsUser">Package Deals</NavLink>
        <NavLink href="/login">Logout</NavLink>
      </Nav>
    </Navbar>
  );
};

export default UserNavbar;
