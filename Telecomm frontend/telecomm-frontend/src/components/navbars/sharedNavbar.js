import React from "react";
import { Nav, NavLink, Navbar, Container } from "reactstrap";

const SharedNavbar = () => {
  return (
    <Navbar color="dark" dark expand="md" style={{ fontSize: "20px" }}>
      <Container>
        <Nav className="me-auto">
          <NavLink href="/" className="text-white">
            <strong>Telekom SEP</strong>
          </NavLink>
        </Nav>
        <Nav>
          <NavLink href="/login" className="text-white">Login</NavLink>
          <NavLink href="/registration" className="text-white">Registration</NavLink>
        </Nav>
      </Container>
    </Navbar>
  );
};

export default SharedNavbar;