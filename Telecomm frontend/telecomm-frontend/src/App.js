import "./App.css";
import { RouterProvider, createBrowserRouter } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import Layout from "./components/layout/Layout";
import ProtectedRoute from "./components/common/ProtectedRoute";
import Home from "./components/home/Home";
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
      element: <Layout />,
      children: [
        {
          path: "/",
          element: <Home />,
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
          element: (
            <ProtectedRoute requiredUserType="SuperAdmin">
              <PackageDealsAdmin />
            </ProtectedRoute>
          ),
        },
        {
          path: "/packageDealsUser",
          element: (
            <ProtectedRoute>
              <PackageDealsUser />
            </ProtectedRoute>
          ),
        },
        {
          path: "/paymentTypes",
          element: (
            <ProtectedRoute>
              <PaymentTypes />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/status",
          element: (
            <ProtectedRoute>
              <PaymentStatusPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/success",
          element: (
            <ProtectedRoute>
              <PaymentSuccessPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/cancel",
          element: (
            <ProtectedRoute>
              <PaymentCancelPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/failed",
          element: (
            <ProtectedRoute>
              <PaymentStatusPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/error",
          element: (
            <ProtectedRoute>
              <PaymentStatusPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/bank/payment",
          element: (
            <ProtectedRoute>
              <BankPaymentPage />
            </ProtectedRoute>
          ),
        },
        {
          path: "/packages",
          element: (
            <ProtectedRoute>
              <PackageDealsUser />
            </ProtectedRoute>
          ),
        },
        {
          path: "/payment/flow",
          element: (
            <ProtectedRoute>
              <PaymentFlow />
            </ProtectedRoute>
          ),
        },
      ],
    },
  ]);

  return (
    <AuthProvider>
      <RouterProvider router={router}></RouterProvider>
    </AuthProvider>
  );
}

export default App;
