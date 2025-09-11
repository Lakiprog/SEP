using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace BankService.Services
{
    public class QRCodeService
    {
        public string GenerateQRCode(string data)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeBytes = qrCode.GetGraphic(20);
                        return Convert.ToBase64String(qrCodeBytes);
                    }
                }
            }
            catch
            {
                // Fallback to simple base64 encoding if QR generation fails
                var qrBytes = System.Text.Encoding.UTF8.GetBytes(data);
                return Convert.ToBase64String(qrBytes);
            }
        }

        public string GeneratePaymentQRCode(decimal amount, string currency, string accountNumber, string receiverName, string orderId)
        {
            // Format QR code data according to NBS IPS specification
            // NBS IPS QR kod format prema specifikaciji sa slike
            var qrData = new
            {
                Version = "0002",
                CharacterSet = "1",
                BIC = "AIKBRSBG", // BIC banke - trebalo bi biti konfigurabilno
                AccountNumber = accountNumber,
                Amount = amount.ToString("F2"),
                Currency = currency,
                Purpose = "AC01", // Kod svrhe plaćanja
                ReceiverName = receiverName,
                Reference = orderId,
                Model = "97" // Model za referencu plaćanja
            };

            // Kreiranje NBS IPS QR koda format stringa (ISO 20022 standard)
            // Format: BCD\nVersion\nCharacterSet\nBIC\nReceiverName\nAccountNumber\nModel\nReference\nPurpose\nAmount\nCurrency
            var qrString = $"BCD\n{qrData.Version}\n{qrData.CharacterSet}\n{qrData.BIC}\n{qrData.ReceiverName}\n{qrData.AccountNumber}\n{qrData.Model}\n{qrData.Reference}\n{qrData.Purpose}\n{qrData.Amount}\n{qrData.Currency}";

            return GenerateQRCode(qrString);
        }

        public bool ValidateQRCode(string qrCodeData)
        {
            try
            {
                // Validacija NBS IPS QR koda format prema specifikaciji
                var lines = qrCodeData.Split('\n');
                
                // Proverava da li počinje sa BCD (EMV QR Code identifikator)
                if (lines.Length < 11 || !lines[0].Equals("BCD"))
                {
                    return false;
                }

                // Validacija verzije (treba da bude 0002 za trenutni standard)
                if (lines[1] != "0002")
                {
                    return false;
                }

                // Validacija karakter seta (1 = UTF-8)
                if (lines[2] != "1")
                {
                    return false;
                }

                // Validacija BIC koda (treba da bude validan BIC format)
                var bic = lines[3];
                if (string.IsNullOrEmpty(bic) || bic.Length < 8 || bic.Length > 11)
                {
                    return false;
                }

                // Validacija naziva primaoca (ne sme biti prazan)
                var receiverName = lines[4];
                if (string.IsNullOrEmpty(receiverName))
                {
                    return false;
                }

                // Validacija broja računa (treba da bude numerički)
                var accountNumber = lines[5];
                if (string.IsNullOrEmpty(accountNumber) || !accountNumber.All(char.IsDigit))
                {
                    return false;
                }

                // Validacija modela (treba da bude 97 ili 26)
                var model = lines[6];
                if (model != "97" && model != "26")
                {
                    return false;
                }

                // Validacija reference (ne sme biti prazna)
                var reference = lines[7];
                if (string.IsNullOrEmpty(reference))
                {
                    return false;
                }

                // Validacija svrhe plaćanja
                var purpose = lines[8];
                if (string.IsNullOrEmpty(purpose))
                {
                    return false;
                }

                // Validacija iznosa (treba da bude numerički i pozitivan)
                var amount = lines[9];
                if (string.IsNullOrEmpty(amount) || !decimal.TryParse(amount, out var amountValue) || amountValue <= 0)
                {
                    return false;
                }

                // Validacija valute (treba da bude RSD za srpske dinare)
                var currency = lines[10];
                if (string.IsNullOrEmpty(currency) || currency.Length != 3)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public (bool IsValid, List<string> Errors) ValidateQRCodeDetailed(string qrCodeData)
        {
            var errors = new List<string>();
            
            try
            {
                var lines = qrCodeData.Split('\n');
                
                // Proverava da li počinje sa BCD
                if (lines.Length < 11 || !lines[0].Equals("BCD"))
                {
                    errors.Add("QR kod mora počinjati sa 'BCD'");
                }

                // Validacija verzije
                if (lines.Length > 1 && lines[1] != "0002")
                {
                    errors.Add("Verzija mora biti '0002'");
                }

                // Validacija karakter seta
                if (lines.Length > 2 && lines[2] != "1")
                {
                    errors.Add("Karakter set mora biti '1' (UTF-8)");
                }

                // Validacija BIC koda
                if (lines.Length > 3)
                {
                    var bic = lines[3];
                    if (string.IsNullOrEmpty(bic))
                    {
                        errors.Add("BIC kod je obavezan");
                    }
                    else if (bic.Length < 8 || bic.Length > 11)
                    {
                        errors.Add("BIC kod mora imati između 8 i 11 karaktera");
                    }
                }

                // Validacija naziva primaoca
                if (lines.Length > 4)
                {
                    var receiverName = lines[4];
                    if (string.IsNullOrEmpty(receiverName))
                    {
                        errors.Add("Naziv primaoca je obavezan");
                    }
                }

                // Validacija broja računa
                if (lines.Length > 5)
                {
                    var accountNumber = lines[5];
                    if (string.IsNullOrEmpty(accountNumber))
                    {
                        errors.Add("Broj računa je obavezan");
                    }
                    else if (!accountNumber.All(char.IsDigit))
                    {
                        errors.Add("Broj računa mora sadržavati samo brojeve");
                    }
                }

                // Validacija modela
                if (lines.Length > 6)
                {
                    var model = lines[6];
                    if (model != "97" && model != "26")
                    {
                        errors.Add("Model mora biti '97' ili '26'");
                    }
                }

                // Validacija reference
                if (lines.Length > 7)
                {
                    var reference = lines[7];
                    if (string.IsNullOrEmpty(reference))
                    {
                        errors.Add("Referenca je obavezna");
                    }
                }

                // Validacija svrhe plaćanja
                if (lines.Length > 8)
                {
                    var purpose = lines[8];
                    if (string.IsNullOrEmpty(purpose))
                    {
                        errors.Add("Svrha plaćanja je obavezna");
                    }
                }

                // Validacija iznosa
                if (lines.Length > 9)
                {
                    var amount = lines[9];
                    if (string.IsNullOrEmpty(amount))
                    {
                        errors.Add("Iznos je obavezan");
                    }
                    else if (!decimal.TryParse(amount, out var amountValue) || amountValue <= 0)
                    {
                        errors.Add("Iznos mora biti validan pozitivan broj");
                    }
                }

                // Validacija valute
                if (lines.Length > 10)
                {
                    var currency = lines[10];
                    if (string.IsNullOrEmpty(currency))
                    {
                        errors.Add("Valuta je obavezna");
                    }
                    else if (currency.Length != 3)
                    {
                        errors.Add("Valuta mora imati tačno 3 karaktera");
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Greška prilikom validacije: {ex.Message}");
                return (false, errors);
            }
        }
    }
}
