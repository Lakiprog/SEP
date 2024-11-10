import React from "react";
import { Nav, NavLink, Navbar } from "reactstrap";

const HomeNavbar = (props) => {
  return (
    <Navbar color="dark" light expand="md" style={{ fontSize: "25px" }}>
      <Nav>
        <NavLink href="/login">Login</NavLink>
        <NavLink href="/registration">Registration</NavLink>
      </Nav>
    </Navbar>
  );
};

export default HomeNavbar;
