import React from "react";
import { Outlet } from "react-router-dom";
import SharedNavbar from "../navbars/sharedNavbar";

const Layout = () => {
  return (
    <div>
      <SharedNavbar />
      <Outlet />
    </div>
  );
};

export default Layout;