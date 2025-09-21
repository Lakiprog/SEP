import React, { useState, useEffect } from 'react';

const CountdownTimer = ({ expiresAt, onExpire, className = '' }) => {
  const [timeLeft, setTimeLeft] = useState(null);
  const [isExpired, setIsExpired] = useState(false);

  useEffect(() => {
    if (!expiresAt) return;

    const updateTimer = () => {
      const now = new Date().getTime();
      const expireTime = new Date(expiresAt).getTime();
      const difference = expireTime - now;

      if (difference <= 0) {
        setTimeLeft(null);
        setIsExpired(true);
        if (onExpire) {
          onExpire();
        }
      } else {
        const minutes = Math.floor((difference % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((difference % (1000 * 60)) / 1000);
        setTimeLeft({ minutes, seconds });
        setIsExpired(false);
      }
    };

    // Update immediately
    updateTimer();

    // Update every second
    const interval = setInterval(updateTimer, 1000);

    return () => clearInterval(interval);
  }, [expiresAt, onExpire]);

  const formatTime = (num) => num.toString().padStart(2, '0');

  if (isExpired) {
    return (
      <div className={`countdown-timer expired ${className}`}>
        <div className="timer-display expired">
          <span className="timer-text">EXPIRED</span>
        </div>
        <div className="timer-message">
          This payment has expired. Please create a new payment.
        </div>
      </div>
    );
  }

  if (!timeLeft) {
    return (
      <div className={`countdown-timer loading ${className}`}>
        <div className="timer-display">
          <span className="timer-text">Loading...</span>
        </div>
      </div>
    );
  }

  const { minutes, seconds } = timeLeft;
  const isUrgent = minutes < 5; // Last 5 minutes

  return (
    <div className={`countdown-timer ${isUrgent ? 'urgent' : ''} ${className}`}>
      <div className="timer-label">Payment expires in:</div>
      <div className={`timer-display ${isUrgent ? 'urgent' : ''}`}>
        <span className="timer-value">{formatTime(minutes)}</span>
        <span className="timer-separator">:</span>
        <span className="timer-value">{formatTime(seconds)}</span>
      </div>
      {isUrgent && (
        <div className="timer-warning">
          ⚠️ Payment will expire soon!
        </div>
      )}
      <style jsx>{`
        .countdown-timer {
          text-align: center;
          padding: 15px;
          border-radius: 8px;
          background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
          border: 2px solid #ddd;
          margin: 15px 0;
        }

        .countdown-timer.urgent {
          background: linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%);
          border-color: #ff6b6b;
          animation: pulse 2s infinite;
        }

        .countdown-timer.expired {
          background: linear-gradient(135deg, #ffcccb 0%, #ff6b6b 100%);
          border-color: #cc0000;
        }

        @keyframes pulse {
          0% { transform: scale(1); }
          50% { transform: scale(1.02); }
          100% { transform: scale(1); }
        }

        .timer-label {
          font-size: 14px;
          color: #666;
          margin-bottom: 8px;
          font-weight: 500;
        }

        .timer-display {
          font-family: 'Courier New', monospace;
          font-size: 28px;
          font-weight: bold;
          color: #333;
          margin: 10px 0;
        }

        .timer-display.urgent {
          color: #d63031;
        }

        .timer-display.expired {
          color: #cc0000;
          font-size: 24px;
        }

        .timer-value {
          background: rgba(255, 255, 255, 0.8);
          padding: 5px 8px;
          border-radius: 4px;
          margin: 0 2px;
        }

        .timer-separator {
          margin: 0 5px;
        }

        .timer-warning {
          font-size: 12px;
          color: #d63031;
          font-weight: bold;
          margin-top: 8px;
        }

        .timer-message {
          font-size: 14px;
          color: #666;
          margin-top: 8px;
        }

        .timer-text {
          background: rgba(255, 255, 255, 0.8);
          padding: 8px 16px;
          border-radius: 4px;
        }
      `}</style>
    </div>
  );
};

export default CountdownTimer;