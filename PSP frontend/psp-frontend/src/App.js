import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import './App.css';

// Components
import Dashboard from './components/Dashboard';
import Merchants from './components/Merchants';
import PaymentMethods from './components/PaymentMethods';
import Transactions from './components/Transactions';
import PaymentSelection from './components/PaymentSelection';
import PSPPaymentSelection from './components/PSPPaymentSelection';
import CustomerPaymentSelection from './components/CustomerPaymentSelection';
import PaymentSelectionLanding from './components/PaymentSelectionLanding';
import TelecomDemo from './components/TelecomDemo';
import WebShop from './components/WebShop';
import AdminAuth from './components/AdminAuth';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/webshop/*" element={<WebShop />} />
          <Route path="/telecom-demo" element={<TelecomDemo />} />
          <Route path="/payment-selection" element={<PaymentSelectionLanding />} />
          <Route path="/payment-selection/:transactionId" element={<CustomerPaymentSelection />} />
          <Route path="/old-payment-selection/:transactionId" element={<PSPPaymentSelection />} />
          <Route path="/*" element={
            <AdminAuth>
              <Routes>
                <Route path="/" element={<Dashboard />} />
                <Route path="/merchants" element={<Merchants />} />
                <Route path="/payment-methods" element={<PaymentMethods />} />
                <Route path="/transactions" element={<Transactions />} />
                <Route path="/admin-payment-selection/:transactionId" element={<PaymentSelection />} />
              </Routes>
            </AdminAuth>
          } />
        </Routes>
        <ToastContainer
          position="top-right"
          autoClose={5000}
          hideProgressBar={false}
          newestOnTop={false}
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
        />
      </div>
    </Router>
  );
}

export default App;
