namespace PaymentServiceProvider.Models
{
    public class PayPalOrderCreationResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("approval_url")]
        public string ApprovalUrl { get; set; } = string.Empty;
    }

    public class PayPalReturnRequest
    {
        public string PSPTransactionId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string PayerID { get; set; } = string.Empty;
    }

    public class PayPalCaptureResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CaptureTime { get; set; } = string.Empty;
    }
}
