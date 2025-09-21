import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import QRPayment from './components/QRPayment';
import CardPayment from './components/CardPayment';
import 'react-toastify/dist/ReactToastify.css';
import './App.css';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/qr-payment" element={<QRPayment />} />
          <Route path="/card-payment" element={<CardPayment />} />
          <Route path="/" element={
            <div className="home-page">
              <h1>🏦 Bank1 Service</h1>
              <p>Bank1 Payment System</p>
            </div>
          } />
        </Routes>
        
        <ToastContainer />
      </div>
    </Router>
  );
}

export default App;
