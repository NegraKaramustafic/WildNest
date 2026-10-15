namespace eCommerce.Model.Exceptions
{
    /// <summary>
    /// Missing entity. The message is shown to the user and must not include
    /// database ids; log the id with <c>ILogger</c> before throwing.
    /// The WebAPI exception filter maps this to HTTP 404
    /// with a JSON body: { "message": "...", "errors": { "notFound": ["..."] } }.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
