import "./App.css";
import { RouterProvider, createBrowserRouter } from "react-router-dom";
import HomeNavbar from "./components/navbars/homeNavbar";
import Registration from "./components/registration/registration";
import Login from "./components/login/login";
import PackageDealsAdmin from "./components/packageDealsAdmin/packageDealsAdmin";
import PackageDealsUser from "./components/packageDealsUser/packageDealsUser";
import PaymentTypes from "./components/paymentTypes/paymentTypes";

function App() {
  const router = createBrowserRouter([
    {
      path: "/",
      element: <HomeNavbar />,
    },
    {
      path: "/login",
      element: <Login />,
    },
    {
      path: "/registration",
      element: <Registration />,
    },
    {
      path: "/packageDealsAdmin",
      element: <PackageDealsAdmin />,
    },
    {
      path: "/packageDealsUser",
      element: <PackageDealsUser />,
    },
    {
      path: "/paymentTypes",
      element: <PaymentTypes />,
    },
  ]);

  return <RouterProvider router={router}></RouterProvider>;
}

export default App;
