namespace eCommerce.Model.Responses
{
    public class ForgotPasswordResponse
    {
        public const string GenericMessage = "If an account exists for that email, a reset code has been sent.";

        public string Message { get; set; } = GenericMessage;

        public int CooldownSeconds { get; set; }
    }
}
