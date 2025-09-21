import React from "react";
import { Nav, NavLink, Navbar, Container, Button } from "reactstrap";
import { useAuth } from "../../contexts/AuthContext";
import { useNavigate } from "react-router-dom";
import { TELECOM_API_BASE_URL } from "../common/constants";
import httpRequest from "../common/httpRequest";

const SharedNavbar = () => {
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = isAuthenticated && user?.userType === "SuperAdmin";

  const handleLogout = async () => {
    try {
      // Call logout endpoint (optional since JWT is stateless)
      await httpRequest.post(`${TELECOM_API_BASE_URL}/User/logout`);
    } catch (error) {
      // Even if server call fails, we still log out locally
      console.error('Logout error:', error);
    } finally {
      logout();
      navigate('/');
    }
  };

  return (
    <Navbar color="dark" dark expand="md" style={{ fontSize: "20px" }}>
      <Container>
        <Nav className="me-auto">
          <NavLink href="/" className="text-white">
            <strong>Telekom SEP</strong>
          </NavLink>
          {isAuthenticated && (
            <>
              <NavLink href="/packageDealsUser" className="text-white">
                Packages
              </NavLink>
              {isAdmin && (
                <NavLink href="/packageDealsAdmin" className="text-white">
                  Admin Panel
                </NavLink>
              )}
            </>
          )}
        </Nav>
        <Nav className="align-items-center">
          {isAuthenticated ? (
            <>
              <span className="text-white me-3">
                Welcome, {user?.username}!
              </span>
              <Button
                color="outline-light"
                size="sm"
                onClick={handleLogout}
              >
                Logout
              </Button>
            </>
          ) : (
            <>
              <NavLink href="/login" className="text-white">Login</NavLink>
              <NavLink href="/registration" className="text-white">Registration</NavLink>
            </>
          )}
        </Nav>
      </Container>
    </Navbar>
  );
};

export default SharedNavbar;