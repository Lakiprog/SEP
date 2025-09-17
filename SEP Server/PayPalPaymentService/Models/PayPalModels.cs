using Newtonsoft.Json;

namespace PayPalPaymentService.Models
{
    // Authentication models
    public class PayPalTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    // Order models
    public class PayPalOrderRequest
    {
        [JsonProperty("intent")]
        public string Intent { get; set; } = "CAPTURE";

        [JsonProperty("purchase_units")]
        public List<PurchaseUnit> PurchaseUnits { get; set; } = new();

        [JsonProperty("application_context")]
        public ApplicationContext ApplicationContext { get; set; } = new();
    }

    public class PurchaseUnit
    {
        [JsonProperty("amount")]
        public Amount Amount { get; set; } = new();

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("custom_id")]
        public string CustomId { get; set; } = string.Empty;
    }

    public class Amount
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; } = "EUR";

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class ApplicationContext
    {
        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; } = string.Empty;

        [JsonProperty("cancel_url")]
        public string CancelUrl { get; set; } = string.Empty;

        [JsonProperty("brand_name")]
        public string BrandName { get; set; } = string.Empty;

        [JsonProperty("locale")]
        public string Locale { get; set; } = "en-US";

        [JsonProperty("landing_page")]
        public string LandingPage { get; set; } = "NO_PREFERENCE";

        [JsonProperty("user_action")]
        public string UserAction { get; set; } = "PAY_NOW";
    }

    public class PayPalOrderResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("links")]
        public List<PayPalLink> Links { get; set; } = new();

        [JsonProperty("create_time")]
        public string CreateTime { get; set; } = string.Empty;

        [JsonProperty("update_time")]
        public string UpdateTime { get; set; } = string.Empty;
    }

    public class PayPalLink
    {
        [JsonProperty("href")]
        public string Href { get; set; } = string.Empty;

        [JsonProperty("rel")]
        public string Rel { get; set; } = string.Empty;

        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;
    }

    // Subscription models
    public class PayPalPlan
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = "ACTIVE";

        [JsonProperty("billing_cycles")]
        public List<BillingCycle> BillingCycles { get; set; } = new();

        [JsonProperty("payment_preferences")]
        public PaymentPreferences PaymentPreferences { get; set; } = new();
    }

    public class BillingCycle
    {
        [JsonProperty("frequency")]
        public Frequency Frequency { get; set; } = new();

        [JsonProperty("tenure_type")]
        public string TenureType { get; set; } = "REGULAR";

        [JsonProperty("sequence")]
        public int Sequence { get; set; } = 1;

        [JsonProperty("total_cycles")]
        public int TotalCycles { get; set; } = 0; // 0 means infinite

        [JsonProperty("pricing_scheme")]
        public PricingScheme PricingScheme { get; set; } = new();
    }

    public class Frequency
    {
        [JsonProperty("interval_unit")]
        public string IntervalUnit { get; set; } = "MONTH";

        [JsonProperty("interval_count")]
        public int IntervalCount { get; set; } = 1;
    }

    public class PricingScheme
    {
        [JsonProperty("fixed_price")]
        public Amount FixedPrice { get; set; } = new();
    }

    public class PaymentPreferences
    {
        [JsonProperty("auto_bill_outstanding")]
        public bool AutoBillOutstanding { get; set; } = true;

        [JsonProperty("setup_fee")]
        public Amount? SetupFee { get; set; }

        [JsonProperty("setup_fee_failure_action")]
        public string SetupFeeFailureAction { get; set; } = "CONTINUE";

        [JsonProperty("payment_failure_threshold")]
        public int PaymentFailureThreshold { get; set; } = 3;
    }

    public class PayPalSubscriptionRequest
    {
        [JsonProperty("plan_id")]
        public string PlanId { get; set; } = string.Empty;

        [JsonProperty("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public string Quantity { get; set; } = "1";

        [JsonProperty("application_context")]
        public SubscriptionApplicationContext ApplicationContext { get; set; } = new();
    }

    public class SubscriptionApplicationContext
    {
        [JsonProperty("brand_name")]
        public string BrandName { get; set; } = string.Empty;

        [JsonProperty("locale")]
        public string Locale { get; set; } = "en-US";

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; } = string.Empty;

        [JsonProperty("cancel_url")]
        public string CancelUrl { get; set; } = string.Empty;

        [JsonProperty("user_action")]
        public string UserAction { get; set; } = "SUBSCRIBE_NOW";
    }

    public class PayPalSubscriptionResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("status_update_time")]
        public string StatusUpdateTime { get; set; } = string.Empty;

        [JsonProperty("plan_id")]
        public string PlanId { get; set; } = string.Empty;

        [JsonProperty("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public string Quantity { get; set; } = string.Empty;

        [JsonProperty("links")]
        public List<PayPalLink> Links { get; set; } = new();

        [JsonProperty("create_time")]
        public string CreateTime { get; set; } = string.Empty;
    }

    // Product model for subscriptions
    public class PayPalProduct
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "SERVICE";

        [JsonProperty("category")]
        public string Category { get; set; } = "SOFTWARE";

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonProperty("home_url")]
        public string HomeUrl { get; set; } = string.Empty;
    }

    public class PayPalProductResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("create_time")]
        public string CreateTime { get; set; } = string.Empty;

        [JsonProperty("update_time")]
        public string UpdateTime { get; set; } = string.Empty;

        [JsonProperty("links")]
        public List<PayPalLink> Links { get; set; } = new();
    }

    // Webhook models
    public class PayPalWebhookEvent
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonProperty("create_time")]
        public string CreateTime { get; set; } = string.Empty;

        [JsonProperty("resource_type")]
        public string ResourceType { get; set; } = string.Empty;

        [JsonProperty("event_version")]
        public string EventVersion { get; set; } = string.Empty;

        [JsonProperty("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonProperty("resource")]
        public object Resource { get; set; } = new();
    }
}
