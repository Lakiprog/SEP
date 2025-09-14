import React, { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import CustomerLayout from './CustomerLayout';

const PaymentSelectionLanding = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  // Auto-redirect if a known query param is present
  useEffect(() => {
    // 1) Try known keys (case-insensitive)
    const knownKeys = ['pspTransactionId', 'pspId', 'transactionId', 'id'];
    for (const key of knownKeys) {
      const value = searchParams.get(key) || searchParams.get(key.toLowerCase()) || searchParams.get(key.toUpperCase());
      if (value && value.trim()) {
        navigate(`/payment-selection/${encodeURIComponent(value.trim())}`, { replace: true });
        return;
      }
    }

    // 2) Fallback: use the first non-empty query param value
    for (const [k, v] of searchParams.entries()) {
      if (v && v.trim()) {
        navigate(`/payment-selection/${encodeURIComponent(v.trim())}`, { replace: true });
        return;
      }
    }

    // 3) Last resort: try to extract from document.referrer query
    try {
      const ref = document.referrer || '';
      const idx = ref.indexOf('?');
      if (idx >= 0) {
        const qs = new URLSearchParams(ref.substring(idx + 1));
        for (const key of knownKeys) {
          const val = qs.get(key) || qs.get(key.toLowerCase()) || qs.get(key.toUpperCase());
          if (val && val.trim()) {
            navigate(`/payment-selection/${encodeURIComponent(val.trim())}`, { replace: true });
            return;
          }
        }
        for (const [k, v] of qs.entries()) {
          if (v && v.trim()) {
            navigate(`/payment-selection/${encodeURIComponent(v.trim())}`, { replace: true });
            return;
          }
        }
      }
    } catch {}
  }, [navigate, searchParams]);
  return (
    <CustomerLayout>
      <div className="customer-payment-container">
        <div className="error" style={{ color: 'white', textAlign: 'center', padding: 40 }}>
          <h2>Payment link is incomplete</h2>
          <p>The payment selection URL is missing a transaction ID.</p>
          <div style={{ marginTop: 20, display: 'flex', gap: 12, justifyContent: 'center' }}>
            <button className="btn btn-secondary" onClick={() => navigate(-1)}>Go Back</button>
          </div>
        </div>
      </div>
    </CustomerLayout>
  );
};

export default PaymentSelectionLanding;
