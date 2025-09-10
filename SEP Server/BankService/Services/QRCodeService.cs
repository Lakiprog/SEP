using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace BankService.Services
{
    public class QRCodeService
    {
        public string GenerateQRCode(string data)
        {
            // Simplified QR code generation for now
            // In real implementation, use QRCoder library
            var qrBytes = System.Text.Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(qrBytes);
        }

        public string GeneratePaymentQRCode(decimal amount, string currency, string accountNumber, string receiverName, string orderId)
        {
            // Format QR code data according to IPS NBS specification
            var qrData = new
            {
                Version = "0002",
                CharacterSet = "1",
                BIC = "AIKBRSBG", // Example BIC
                AccountNumber = accountNumber,
                Amount = amount.ToString("F2"),
                Currency = currency,
                Purpose = "AC01",
                ReceiverName = receiverName,
                Reference = orderId,
                Model = "97"
            };

            // Convert to QR code format string
            var qrString = $"BCD\n{qrData.Version}\n{qrData.CharacterSet}\n{qrData.BIC}\n{qrData.ReceiverName}\n{qrData.AccountNumber}\n{qrData.Model}\n{qrData.Reference}\n{qrData.Purpose}\n{qrData.Amount}\n{qrData.Currency}";

            return GenerateQRCode(qrString);
        }
    }
}
