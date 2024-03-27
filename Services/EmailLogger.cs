namespace SimpleMathQuizzesAPI.Services
{
    /// <summary>
    /// This api does not have an email sender, but the identity api endpoints need an email sender for email and password changes/resets.<br/>
    /// This class allows access to the email/password reset codes by logging it to the console.
    /// </summary>
    /// 
    /// <typeparam name="T">
    /// The user class of the app (User in this case).
    /// </typeparam>
    public class EmailLogger<T> : Microsoft.AspNetCore.Identity.IEmailSender<T> where T : class
    {
        private readonly ILogger<EmailLogger<T>> _logger;

        /// <summary>
        /// Constructor for EmailLogger.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The logger to use for logging the reset code to the console.
        /// </param>
        public EmailLogger(ILogger<EmailLogger<T>> logger)
        {
            _logger = logger;
        }


        public Task SendConfirmationLinkAsync(T user, string email, string confirmationLink)
        {
            _logger.LogInformation("sendConfirmationLinkAsync called");
            return logEmail(email, confirmationLink);
        }

        public Task SendPasswordResetLinkAsync(T user, string email, string resetLink)
        {
            _logger.LogInformation("sendPasswordResetLinkAsync called");
            return logEmail(email, resetLink);
        }

        // This function requires the user's email to be confirmed (EmailConfirmed = true)
        public Task SendPasswordResetCodeAsync(T user, string email, string resetCode)
        {
            _logger.LogInformation("sendPasswordResetCodeAsync called");
            return logEmail(email, resetCode);
        }


        /// <summary>
        /// Logs the email to the console.<br/>
        /// This allows access to the email and password reset codes that Identity sends through email.
        /// </summary>
        /// 
        /// <param name="email">
        /// The email address that the email would be sent to.
        /// </param>
        /// 
        /// <param name="code">
        /// the email or password reset code.
        /// </param>
        /// 
        /// <returns>
        /// A completed task, after logging the email to the console.
        /// </returns>
        public Task logEmail(string email, string code)
        {
            string emailToSend = $"Email to send:\nemail: {email},\n code: {code}".Replace("&amp;", "&");
            _logger.LogInformation(emailToSend);
            // Console.WriteLine(emailToSend);

            return Task.CompletedTask;
        }
    }
}
