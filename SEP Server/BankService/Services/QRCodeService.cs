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

        public string GeneratePaymentQRCode(decimal amount, string currency, string accountNumber, string receiverName, string orderId, string? purpose = null, string? approvalNumber = null)
        {
            // Format QR code data according to NBS IPS specification
            // NBS IPS QR kod format prema specifikaciji sa tagovima
            
            // Required tags
            var qrString = "K:PR|V:01|C:1|";
            
            // Format account number as 18-digit string without dashes
            var formattedAccountNumber = FormatAccountNumber(accountNumber);
            qrString += $"R:{formattedAccountNumber}|";
            
            // Payment receiver name
            var formattedReceiverName = FormatReceiverName(receiverName);
            qrString += $"N:{formattedReceiverName}|";
            
            // Optional tags
            if (amount > 0 && !string.IsNullOrEmpty(currency))
            {
                // Format I:RSD1025,1 or I:RSD1025,12 - with comma according to specification
                var amountStr = amount.ToString("F2").Replace(".", ",");
                qrString += $"I:{currency}{amountStr}|";
            }
            
            // SF tag is always 221 according to specification
            qrString += "SF:221|";
            
            if (!string.IsNullOrEmpty(purpose))
            {
                qrString += $"S:{purpose}|";
            }
            else
            {
                // Default payment purpose if not specified
                qrString += "S:Telekom usluge|";
            }
            
            if (!string.IsNullOrEmpty(orderId))
            {
                var formattedOrderId = FormatApprovalNumber(orderId);
                qrString += $"RO:{formattedOrderId}";
            }
            else if (!string.IsNullOrEmpty(approvalNumber))
            {
                var formattedApprovalNumber = FormatApprovalNumber(approvalNumber);
                qrString += $"RO:{formattedApprovalNumber}";
            }

            return GenerateQRCode(qrString);
        }

        private string FormatAccountNumber(string accountNumber)
        {
            // Uklanjanje svih crtica i razmaka
            var cleanNumber = accountNumber.Replace("-", "").Replace(" ", "");
            
            // Check if it contains only numbers
            if (!cleanNumber.All(char.IsDigit))
            {
                throw new ArgumentException("Account number must contain only numbers");
            }
            
            // Format as 18-digit string with leading zeros
            return cleanNumber.PadLeft(18, '0');
        }

        private string FormatReceiverName(string receiverName)
        {
            if (string.IsNullOrEmpty(receiverName))
            {
                throw new ArgumentException("Naziv primaoca je obavezan");
            }
            
            // Remove leading and trailing spaces
            var trimmedName = receiverName.Trim();
            
            // Replace multiple spaces with single space
            while (trimmedName.Contains("  "))
            {
                trimmedName = trimmedName.Replace("  ", " ");
            }
            
            // Maksimalno 25 karaktera prema specifikaciji
            if (trimmedName.Length > 25)
            {
                trimmedName = trimmedName.Substring(0, 25);
            }
            
            return trimmedName;
        }

        private string FormatApprovalNumber(string approvalNumber)
        {
            if (string.IsNullOrEmpty(approvalNumber))
            {
                throw new ArgumentException("Broj odobrenja ne sme biti prazan");
            }
            
            // Uklanjanje svih karaktera koji nisu cifre
            var cleanNumber = new string(approvalNumber.Where(char.IsDigit).ToArray());
            
            if (string.IsNullOrEmpty(cleanNumber))
            {
                throw new ArgumentException("Approval number must contain at least one digit");
            }
            
            // Maximum 23 digits for cleanNumber because we add "00" at the beginning
            // Ukupno maksimalno 25 cifara (00 + maksimalno 23 cifre)
            if (cleanNumber.Length > 23)
            {
                cleanNumber = cleanNumber.Substring(0, 23);
            }
            return "971425088234221082";
            // Prve dve cifre su uvek 00 prema specifikaciji
            return "00" + cleanNumber;
            
        }

        public bool ValidateQRCode(string qrCodeData)
        {
            try
            {
                // Validacija NBS IPS QR koda format prema specifikaciji
                if (string.IsNullOrEmpty(qrCodeData))
                {
                    return false;
                }

                // Check if it starts with K:PR (NBS IPS QR code identifier)
                if (!qrCodeData.StartsWith("K:PR"))
                {
                    return false;
                }

                // Razdvajanje tagova po delimiteru |
                var tags = qrCodeData.Split('|');
                
                // Proverava obavezne tagove
                bool hasK = false, hasV = false, hasC = false, hasR = false, hasN = false;
                
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    var parts = tag.Split(':');
                    if (parts.Length != 2) continue;
                    
                    var tagName = parts[0];
                    var tagValue = parts[1];
                    
                    switch (tagName)
                    {
                        case "K":
                            if (tagValue != "PR") return false;
                            hasK = true;
                            break;
                        case "V":
                            if (tagValue != "01") return false;
                            hasV = true;
                            break;
                        case "C":
                            if (tagValue != "1") return false;
                            hasC = true;
                            break;
                        case "R":
                            // Account number must be 18-digit numeric string
                            if (string.IsNullOrEmpty(tagValue) || tagValue.Length != 18 || !tagValue.All(char.IsDigit))
                                return false;
                            hasR = true;
                            break;
                        case "N":
                            // Naziv primaoca je obavezan
                            if (string.IsNullOrEmpty(tagValue) || tagValue.Length > 25)
                                return false;
                            hasN = true;
                            break;
                        case "I":
                            // Format I:RSD1025,1 or I:RSD1025,12 - with comma according to specification
                            if (string.IsNullOrEmpty(tagValue) || tagValue.Length < 4)
                                return false;
                            // Check if it starts with currency (3 characters)
                            if (!char.IsLetter(tagValue[0]) || !char.IsLetter(tagValue[1]) || !char.IsLetter(tagValue[2]))
                                return false;
                            // Check if it contains digits and comma
                            var amountPart = tagValue.Substring(3);
                            if (!amountPart.Contains(",") || amountPart.Count(c => c == ',') != 1)
                                return false;
                            break;
                        case "SF":
                            // SF tag mora biti 221
                            if (tagValue != "221")
                                return false;
                            break;
                        case "RO":
                            // RO tag must start with 00 and contain only digits, maximum 25 characters
                            if (string.IsNullOrEmpty(tagValue) || tagValue.Length < 2 || tagValue.Length > 25)
                                return false;
                            if (!tagValue.StartsWith("00") || !tagValue.All(char.IsDigit))
                                return false;
                            break;
                    }
                }

                // Proverava da li su svi obavezni tagovi prisutni
                return hasK && hasV && hasC && hasR && hasN;
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
                if (string.IsNullOrEmpty(qrCodeData))
                {
                    errors.Add("QR kod ne sme biti prazan");
                    return (false, errors);
                }

                // Check if it starts with K:PR
                if (!qrCodeData.StartsWith("K:PR"))
                {
                    errors.Add("QR code must start with 'K:PR'");
                }

                // Razdvajanje tagova po delimiteru |
                var tags = qrCodeData.Split('|');
                
                // Proverava obavezne tagove
                bool hasK = false, hasV = false, hasC = false, hasR = false, hasN = false;
                
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    var parts = tag.Split(':');
                    if (parts.Length != 2)
                    {
                        errors.Add($"Neispravan format taga: {tag}");
                        continue;
                    }
                    
                    var tagName = parts[0];
                    var tagValue = parts[1];
                    
                    switch (tagName)
                    {
                        case "K":
                            if (tagValue != "PR")
                            {
                                errors.Add("Tag K mora imati vrednost 'PR'");
                            }
                            else
                            {
                                hasK = true;
                            }
                            break;
                        case "V":
                            if (tagValue != "01")
                            {
                                errors.Add("Tag V mora imati vrednost '01'");
                            }
                            else
                            {
                                hasV = true;
                            }
                            break;
                        case "C":
                            if (tagValue != "1")
                            {
                                errors.Add("Tag C mora imati vrednost '1' (UTF-8)");
                            }
                            else
                            {
                                hasC = true;
                            }
                            break;
                        case "R":
                            if (string.IsNullOrEmpty(tagValue))
                            {
                                errors.Add("Account number is required");
                            }
                            else if (tagValue.Length != 18)
                            {
                                errors.Add("Account number must have exactly 18 digits");
                            }
                            else if (!tagValue.All(char.IsDigit))
                            {
                                errors.Add("Account number must contain only numbers");
                            }
                            else
                            {
                                hasR = true;
                            }
                            break;
                        case "N":
                            if (string.IsNullOrEmpty(tagValue))
                            {
                                errors.Add("Naziv primaoca je obavezan");
                            }
                            else if (tagValue.Length > 25)
                            {
                                errors.Add("Receiver name cannot be longer than 25 characters");
                            }
                            else
                            {
                                hasN = true;
                            }
                            break;
                        case "I":
                            if (string.IsNullOrEmpty(tagValue))
                            {
                                errors.Add("Iznos ne sme biti prazan");
                            }
                            else if (tagValue.Length < 4)
                            {
                                errors.Add("Iznos mora imati format valuta+cifre,zarez+cifre (npr. RSD1025,1)");
                            }
                            else if (!char.IsLetter(tagValue[0]) || !char.IsLetter(tagValue[1]) || !char.IsLetter(tagValue[2]))
                            {
                                errors.Add("Amount must start with currency (3 letters)");
                            }
                            else
                            {
                                var amountPart = tagValue.Substring(3);
                                if (!amountPart.Contains(",") || amountPart.Count(c => c == ',') != 1)
                                {
                                    errors.Add("Amount must contain exactly one comma (e.g. RSD1025,1)");
                                }
                            }
                            break;
                        case "SF":
                            if (tagValue != "221")
                            {
                                errors.Add("SF tag mora imati vrednost '221'");
                            }
                            break;
                        case "S":
                            if (string.IsNullOrEmpty(tagValue))
                            {
                                errors.Add("Payment purpose cannot be empty");
                            }
                            break;
                        case "RO":
                            if (string.IsNullOrEmpty(tagValue))
                            {
                                errors.Add("Referenca ne sme biti prazna");
                            }
                            else if (tagValue.Length < 2 || tagValue.Length > 25)
                            {
                                errors.Add("Reference must have between 2 and 25 characters");
                            }
                            else if (!tagValue.StartsWith("00"))
                            {
                                errors.Add("Reference must start with '00'");
                            }
                            else if (!tagValue.All(char.IsDigit))
                            {
                                errors.Add("Reference must contain only digits");
                            }
                            break;
                        default:
                            errors.Add($"Nepoznat tag: {tagName}");
                            break;
                    }
                }

                // Proverava da li su svi obavezni tagovi prisutni
                if (!hasK) errors.Add("Obavezan tag K je nedostaje");
                if (!hasV) errors.Add("Obavezan tag V je nedostaje");
                if (!hasC) errors.Add("Obavezan tag C je nedostaje");
                if (!hasR) errors.Add("Obavezan tag R je nedostaje");
                if (!hasN) errors.Add("Obavezan tag N je nedostaje");

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                return (false, errors);
            }
        }

        public Dictionary<string, string> ParseQRCode(string qrCodeData)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                if (string.IsNullOrEmpty(qrCodeData))
                {
                    return result;
                }

                // Razdvajanje tagova po delimiteru |
                var tags = qrCodeData.Split('|');
                
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    var parts = tag.Split(':');
                    if (parts.Length == 2)
                    {
                        result[parts[0]] = parts[1];
                    }
                }
            }
            catch
            {
                // Return empty dictionary in case of error
            }
            
            return result;
        }

        public string GenerateQRCodeExample()
        {
            // Primer generisanja NBS IPS QR koda prema specifikaciji
            return GeneratePaymentQRCode(
                amount: 49.99m,
                currency: "RSD",
                accountNumber: "105-0000000000999-39", // Will be formatted as 18-digit string
                receiverName: "Telekom Srbija",
                orderId: "69007399344596557495215",
                purpose: "Telekom paket Premium"
            );
        }
    }
}
