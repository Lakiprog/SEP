import React, { useState } from 'react';

const QRCodeDisplay = ({
  qrCodeImage,
  qrCodeData,
  address,
  amount,
  currency,
  className = ''
}) => {
  const [copied, setCopied] = useState(false);

  const copyToClipboard = async (text, type) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(type);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
      // Fallback for older browsers
      const textArea = document.createElement('textarea');
      textArea.value = text;
      document.body.appendChild(textArea);
      textArea.select();
      document.execCommand('copy');
      document.body.removeChild(textArea);
      setCopied(type);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const formatCurrency = (curr) => {
    return curr ? curr.toUpperCase() : 'BTC';
  };

  const formatAmount = (amt) => {
    return typeof amt === 'number' ? amt.toFixed(8) : amt;
  };

  const truncateAddress = (addr) => {
    if (!addr) return '';
    return addr.length > 20 ? `${addr.slice(0, 10)}...${addr.slice(-10)}` : addr;
  };

  return (
    <div className={`qr-code-display ${className}`}>
      <div className="qr-section">
        <h3>Scan QR Code to Pay</h3>

        {qrCodeImage ? (
          <div className="qr-container">
            <img
              src={`data:image/png;base64,${qrCodeImage}`}
              alt="QR Code for crypto payment"
              className="qr-image"
            />
          </div>
        ) : (
          <div className="qr-placeholder">
            <div className="loading-spinner">Loading QR Code...</div>
          </div>
        )}

        <div className="payment-details">
          <div className="detail-row">
            <span className="label">Amount:</span>
            <span className="value">{formatAmount(amount)} {formatCurrency(currency)}</span>
          </div>

          {address && (
            <div className="detail-row">
              <span className="label">Address:</span>
              <div className="address-container">
                <span className="value address" title={address}>
                  {truncateAddress(address)}
                </span>
                <button
                  className={`copy-btn ${copied === 'address' ? 'copied' : ''}`}
                  onClick={() => copyToClipboard(address, 'address')}
                  title="Copy address"
                >
                  {copied === 'address' ? '✓' : '📋'}
                </button>
              </div>
            </div>
          )}

          {qrCodeData && (
            <div className="detail-row">
              <span className="label">Payment URI:</span>
              <div className="address-container">
                <span className="value uri" title={qrCodeData}>
                  {truncateAddress(qrCodeData)}
                </span>
                <button
                  className={`copy-btn ${copied === 'uri' ? 'copied' : ''}`}
                  onClick={() => copyToClipboard(qrCodeData, 'uri')}
                  title="Copy payment URI"
                >
                  {copied === 'uri' ? '✓' : '📋'}
                </button>
              </div>
            </div>
          )}
        </div>

        <div className="instructions">
          <p>📱 <strong>Mobile Wallet:</strong> Scan the QR code with your crypto wallet app</p>
          <p>💻 <strong>Desktop Wallet:</strong> Copy the address or payment URI to your wallet</p>
        </div>
      </div>

      <style jsx>{`
        .qr-code-display {
          background: white;
          border-radius: 12px;
          padding: 24px;
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
          border: 1px solid #e1e5e9;
          max-width: 400px;
          margin: 0 auto;
        }

        .qr-section h3 {
          text-align: center;
          color: #2c3e50;
          margin: 0 0 20px 0;
          font-size: 18px;
        }

        .qr-container {
          display: flex;
          justify-content: center;
          margin: 20px 0;
          padding: 15px;
          background: #f8f9fa;
          border-radius: 8px;
          border: 2px dashed #dee2e6;
        }

        .qr-image {
          max-width: 200px;
          max-height: 200px;
          border-radius: 8px;
          background: white;
          padding: 10px;
        }

        .qr-placeholder {
          display: flex;
          justify-content: center;
          align-items: center;
          height: 220px;
          background: #f8f9fa;
          border-radius: 8px;
          border: 2px dashed #dee2e6;
          margin: 20px 0;
        }

        .loading-spinner {
          color: #6c757d;
          font-style: italic;
        }

        .payment-details {
          background: #f8f9fa;
          border-radius: 8px;
          padding: 16px;
          margin: 16px 0;
        }

        .detail-row {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 12px;
          flex-wrap: wrap;
        }

        .detail-row:last-child {
          margin-bottom: 0;
        }

        .label {
          font-weight: 600;
          color: #495057;
          min-width: 80px;
        }

        .value {
          color: #212529;
          font-family: 'Courier New', monospace;
          word-break: break-all;
          flex: 1;
          margin-right: 8px;
        }

        .address-container {
          display: flex;
          align-items: center;
          flex: 1;
          min-width: 0;
        }

        .copy-btn {
          background: #007bff;
          color: white;
          border: none;
          border-radius: 4px;
          padding: 4px 8px;
          cursor: pointer;
          font-size: 12px;
          transition: all 0.2s;
          min-width: 30px;
          height: 24px;
        }

        .copy-btn:hover {
          background: #0056b3;
          transform: scale(1.05);
        }

        .copy-btn.copied {
          background: #28a745;
        }

        .instructions {
          background: #e3f2fd;
          border-radius: 8px;
          padding: 16px;
          margin-top: 16px;
        }

        .instructions p {
          margin: 8px 0;
          font-size: 14px;
          color: #1565c0;
          line-height: 1.4;
        }

        .instructions strong {
          color: #0d47a1;
        }

        @media (max-width: 480px) {
          .qr-code-display {
            padding: 16px;
            margin: 10px;
          }

          .detail-row {
            flex-direction: column;
            align-items: flex-start;
          }

          .address-container {
            width: 100%;
            margin-top: 4px;
          }

          .qr-image {
            max-width: 180px;
            max-height: 180px;
          }
        }
      `}</style>
    </div>
  );
};

export default QRCodeDisplay;