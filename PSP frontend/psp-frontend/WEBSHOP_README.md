# WebShop Portal

The WebShop Portal allows merchants to log in and manage their payment methods for their online stores.

## Features

### 🔐 Authentication
- Secure login with Merchant ID and Password
- Session management with tokens
- Automatic logout functionality

### 📊 Dashboard
- Overview of payment methods count
- Transaction statistics (total, completed, volume)
- Recent transactions list
- Real-time data updates

### 💳 Payment Methods Management
- View all available payment methods
- Enable/disable payment methods for your store
- Real-time updates when saving changes
- Visual toggle switches for easy management

### 📈 Transaction History
- Complete transaction history
- Filter by status (completed, pending, failed)
- Payment method information
- Date and amount details

## How to Use

### 1. Access the WebShop Portal
- Navigate to `/webshop` in your browser
- Or click "WebShop Portal" in the admin sidebar

### 2. Login
Use the demo credentials:
- **Merchant ID:** `TELECOM_001`
- **Password:** `telecom123`

### 3. Manage Payment Methods
- Go to the "Payment Methods" tab
- Toggle payment methods on/off
- Click "Save Changes" to apply

### 4. View Dashboard
- Check your statistics in the "Overview" tab
- Monitor recent transactions
- Track your payment volume

## Available Payment Methods

The system supports the following payment methods:
- **Credit/Debit Card** - Traditional card payments
- **PayPal** - PayPal account payments  
- **Bitcoin** - Cryptocurrency payments

## API Endpoints

The WebShop Portal uses the following API endpoints:

### Authentication
- `POST /api/webshop/login` - Login with credentials
- `POST /api/webshop/validate-token` - Validate session token

### Payment Methods
- `GET /api/webshop/{clientId}/payment-methods` - Get available payment methods
- `POST /api/webshop/{clientId}/payment-methods` - Update payment methods

### Dashboard
- `GET /api/webshop/{clientId}/dashboard` - Get dashboard data

## Security Features

- CORS protection configured for frontend origins
- Session token validation
- Secure password handling
- HTTPS support

## Technical Details

### Frontend Components
- `WebShop.js` - Main container component
- `WebShopLogin.js` - Login form component
- `WebShopDashboard.js` - Dashboard and settings component

### Backend Components
- `WebShopAuthController.cs` - Authentication and management endpoints
- Enhanced `WebShopClientService.cs` - Business logic
- Updated `WebShopClientRepository.cs` - Data access layer

### Styling
- Modern, responsive design
- Mobile-friendly interface
- Consistent with admin panel theme
- Smooth animations and transitions

## Development Notes

- The system uses React Router for navigation
- State management with React hooks
- Axios for API communication
- CSS modules for styling
- Font Awesome icons for UI elements

## Future Enhancements

- Advanced transaction filtering
- Export functionality for reports
- Real-time notifications
- Multi-language support
- Advanced analytics and charts
