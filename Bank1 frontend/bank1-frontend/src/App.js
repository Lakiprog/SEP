import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import QRPayment from './components/QRPayment';
import 'react-toastify/dist/ReactToastify.css';
import './App.css';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/qr-payment" element={<QRPayment />} />
          <Route path="/" element={
            <div className="home-page">
              <h1>🏦 Bank1 Service</h1>
              <p>Dobrodošli u Bank1 plaćanja sistem</p>
              <p>Za QR plaćanje idite na /qr-payment</p>
            </div>
          } />
        </Routes>
        
        <ToastContainer />
      </div>
    </Router>
  );
}

export default App;
