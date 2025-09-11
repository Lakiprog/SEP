import "./App.css";
import { RouterProvider, createBrowserRouter } from "react-router-dom";
import HomeNavbar from "./components/navbars/homeNavbar";
import Registration from "./components/registration/registration";
import Login from "./components/login/login";
import PackageDealsAdmin from "./components/packageDealsAdmin/packageDealsAdmin";
import PackageDealsUser from "./components/packageDealsUser/packageDealsUser";
import PaymentTypes from "./components/paymentTypes/paymentTypes";
import PaymentStatus from "./components/paymentContainer/PaymentStatus";
import BankPaymentPage from "./components/bankPaymentPage/bankPaymentPage";
import PaymentStatusPage from "./components/paymentStatusPage/paymentStatusPage";
import PaymentSuccessPage from "./components/paymentSuccessPage/paymentSuccessPage";
import PaymentCancelPage from "./components/paymentCancelPage/paymentCancelPage";
import PaymentFlow from "./components/paymentFlow/paymentFlow";

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
    {
      path: "/payment/status",
      element: <PaymentStatusPage />,
    },
    {
      path: "/payment/success",
      element: <PaymentSuccessPage />,
    },
    {
      path: "/payment/cancel",
      element: <PaymentCancelPage />,
    },
    {
      path: "/payment/failed",
      element: <PaymentStatusPage />,
    },
    {
      path: "/payment/error",
      element: <PaymentStatusPage />,
    },
    {
      path: "/bank/payment",
      element: <BankPaymentPage />,
    },
    {
      path: "/packages",
      element: <PackageDealsUser />,
    },
    {
      path: "/payment/flow",
      element: <PaymentFlow />,
    },
  ]);

  return <RouterProvider router={router}></RouterProvider>;
}

export default App;
