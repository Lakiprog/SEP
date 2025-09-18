using Newtonsoft.Json;

namespace BitcoinPaymentService.Models
{
    public class CoinPaymentsApiResponse<T>
    {
        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("result")]
        public T? Result { get; set; }
    }

    public class CreateTransactionRequest
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("currency1")]
        public string Currency1 { get; set; } = "USD";

        [JsonProperty("currency2")]
        public string Currency2 { get; set; } = "BTC";

        [JsonProperty("buyer_email")]
        public string BuyerEmail { get; set; } = string.Empty;

        [JsonProperty("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonProperty("item_number")]
        public string ItemNumber { get; set; } = string.Empty;

        [JsonProperty("custom")]
        public string Custom { get; set; } = string.Empty;

        [JsonProperty("ipn_url")]
        public string IpnUrl { get; set; } = string.Empty;
    }

    public class CreateTransactionResponse
    {
        [JsonProperty("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonProperty("address")]
        public string Address { get; set; } = string.Empty;

        [JsonProperty("dest_tag")]
        public string DestTag { get; set; } = string.Empty;

        [JsonProperty("txn_id")]
        public string TxnId { get; set; } = string.Empty;

        [JsonProperty("confirms_needed")]
        public string ConfirmsNeeded { get; set; } = string.Empty;

        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        [JsonProperty("status_url")]
        public string StatusUrl { get; set; } = string.Empty;

        [JsonProperty("qrcode_url")]
        public string QrcodeUrl { get; set; } = string.Empty;
    }

    public class GetTransactionInfoResponse
    {
        [JsonProperty("time_created")]
        public long TimeCreated { get; set; }

        [JsonProperty("time_expires")]
        public long TimeExpires { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("status_text")]
        public string StatusText { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("coin")]
        public string Coin { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("amountf")]
        public string AmountF { get; set; } = string.Empty;

        [JsonProperty("received")]
        public decimal Received { get; set; }

        [JsonProperty("receivedf")]
        public string ReceivedF { get; set; } = string.Empty;

        [JsonProperty("recv_confirms")]
        public int RecvConfirms { get; set; }

        [JsonProperty("payment_address")]
        public string PaymentAddress { get; set; } = string.Empty;
    }

    public class CurrencyInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class QRCodeRequest
    {
        public string Currency { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class QRCodeResponse
    {
        public string QRCodeData { get; set; } = string.Empty;
        public string QRCodeImage { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }

    public class PaymentTimeout
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    // Invoice API Models
    public class CreateInvoiceRequest
    {
        [JsonProperty("currency")]
        public string Currency { get; set; } = "LTC";

        [JsonProperty("items")]
        public List<InvoiceItem> Items { get; set; } = new();

        [JsonProperty("amount")]
        public InvoiceAmount Amount { get; set; } = new();

        [JsonProperty("isEmailDelivery")]
        public bool IsEmailDelivery { get; set; } = false;

        [JsonProperty("emailDelivery")]
        public EmailDelivery? EmailDelivery { get; set; }

        [JsonProperty("dueDate")]
        public string DueDate { get; set; } = string.Empty;

        [JsonProperty("invoiceDate")]
        public string InvoiceDate { get; set; } = string.Empty;

        [JsonProperty("draft")]
        public bool Draft { get; set; } = false;

        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        [JsonProperty("buyer")]
        public InvoiceBuyer? Buyer { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("shipping")]
        public InvoiceShipping? Shipping { get; set; }

        [JsonProperty("requireBuyerNameAndEmail")]
        public bool RequireBuyerNameAndEmail { get; set; } = false;

        [JsonProperty("buyerDataCollectionMessage")]
        public string BuyerDataCollectionMessage { get; set; } = string.Empty;

        [JsonProperty("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonProperty("notesToRecipient")]
        public string NotesToRecipient { get; set; } = string.Empty;

        [JsonProperty("termsAndConditions")]
        public string TermsAndConditions { get; set; } = string.Empty;

        [JsonProperty("merchantOptions")]
        public MerchantOptions? MerchantOptions { get; set; }

        [JsonProperty("customData")]
        public Dictionary<string, string>? CustomData { get; set; }

        [JsonProperty("poNumber")]
        public string PoNumber { get; set; } = string.Empty;

        [JsonProperty("webhooks")]
        public List<InvoiceWebhook>? Webhooks { get; set; }

        [JsonProperty("payoutOverrides")]
        public List<PayoutOverride>? PayoutOverrides { get; set; }

        [JsonProperty("payment")]
        public InvoicePayment? Payment { get; set; }

        [JsonProperty("hideShoppingCart")]
        public bool HideShoppingCart { get; set; } = false;
    }

    public class InvoiceItem
    {
        [JsonProperty("customId")]
        public string CustomId { get; set; } = string.Empty;

        [JsonProperty("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public InvoiceQuantity Quantity { get; set; } = new();

        [JsonProperty("originalAmount")]
        public string OriginalAmount { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonProperty("tax")]
        public string Tax { get; set; } = string.Empty;
    }

    public class InvoiceQuantity
    {
        [JsonProperty("value")]
        public int Value { get; set; } = 1;

        [JsonProperty("type")]
        public string Type { get; set; } = "2"; // 1 = hours, 2 = units
    }

    public class InvoiceAmount
    {
        [JsonProperty("breakdown")]
        public AmountBreakdown Breakdown { get; set; } = new();

        [JsonProperty("total")]
        public string Total { get; set; } = string.Empty;
    }

    public class AmountBreakdown
    {
        [JsonProperty("subtotal")]
        public string Subtotal { get; set; } = string.Empty;

        [JsonProperty("shipping")]
        public string Shipping { get; set; } = string.Empty;

        [JsonProperty("handling")]
        public string Handling { get; set; } = string.Empty;

        [JsonProperty("taxTotal")]
        public string TaxTotal { get; set; } = string.Empty;

        [JsonProperty("discount")]
        public string Discount { get; set; } = string.Empty;
    }

    public class EmailDelivery
    {
        [JsonProperty("to")]
        public string To { get; set; } = string.Empty;

        [JsonProperty("cc")]
        public string Cc { get; set; } = string.Empty;

        [JsonProperty("bcc")]
        public string Bcc { get; set; } = string.Empty;
    }

    public class InvoiceBuyer
    {
        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public InvoiceName? Name { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonProperty("address")]
        public InvoiceAddress? Address { get; set; }
    }

    public class InvoiceName
    {
        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;
    }

    public class InvoiceAddress
    {
        [JsonProperty("address1")]
        public string Address1 { get; set; } = string.Empty;

        [JsonProperty("address2")]
        public string Address2 { get; set; } = string.Empty;

        [JsonProperty("address3")]
        public string Address3 { get; set; } = string.Empty;

        [JsonProperty("provinceOrState")]
        public string ProvinceOrState { get; set; } = string.Empty;

        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;

        [JsonProperty("suburbOrDistrict")]
        public string SuburbOrDistrict { get; set; } = string.Empty;

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; } = string.Empty;

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; } = string.Empty;
    }

    public class InvoiceShipping
    {
        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public InvoiceName? Name { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonProperty("address")]
        public InvoiceAddress? Address { get; set; }
    }

    public class MerchantOptions
    {
        [JsonProperty("showAddress")]
        public bool ShowAddress { get; set; } = false;

        [JsonProperty("showEmail")]
        public bool ShowEmail { get; set; } = false;

        [JsonProperty("showPhone")]
        public bool ShowPhone { get; set; } = false;

        [JsonProperty("showRegistrationNumber")]
        public bool ShowRegistrationNumber { get; set; } = false;

        [JsonProperty("additionalInfo")]
        public string AdditionalInfo { get; set; } = string.Empty;
    }

    public class InvoiceWebhook
    {
        [JsonProperty("notificationsUrl")]
        public string NotificationsUrl { get; set; } = string.Empty;

        [JsonProperty("notifications")]
        public List<string> Notifications { get; set; } = new();
    }

    public class PayoutOverride
    {
        [JsonProperty("fromCurrency")]
        public string FromCurrency { get; set; } = "LTCT";

        [JsonProperty("toCurrency")]
        public string ToCurrency { get; set; } = "LTCT";

        [JsonProperty("address")]
        public string Address { get; set; } = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec";

        [JsonProperty("frequency")]
        public List<string> Frequency { get; set; } = new();
    }

    public class InvoicePayment
    {
        [JsonProperty("paymentCurrency")]
        public string PaymentCurrency { get; set; } = string.Empty;

        [JsonProperty("refundEmail")]
        public string RefundEmail { get; set; } = "colakdarie@gmail.com";
    }

    public class CreateInvoiceResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}