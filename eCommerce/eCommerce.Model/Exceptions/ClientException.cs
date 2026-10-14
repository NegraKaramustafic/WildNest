namespace eCommerce.Model.Exceptions
{
    /// <summary>
    /// User-facing business rule violation. The WebAPI exception filter maps this to HTTP 400
    /// with a JSON body: { "message": "...", "errors": { "clientError": ["..."] } }.
    /// </summary>
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message)
        {
        }

        public ClientException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
