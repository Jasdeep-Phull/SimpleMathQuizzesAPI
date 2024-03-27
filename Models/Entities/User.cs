using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleMathQuizzesAPI.Entities
{
    /// <summary>
    /// Class representing users.<br/>
    /// Inherits from IdentityUser, and adds the User.Quizzes navigational property.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// Constructor for the User class.<br/>
        /// This initialises the quizzes property as an empty list, to avoid NullReferenceExceptions when accessing user.Quizzes.
        /// </summary>
        public User() : base()
        {
            // initialise quizzes as an empty list, to avoid NullReferenceExceptions when accessing user.Quizzes
            Quizzes = [];

            /* RequireConfirmedEmail is set to false in the AddIdentityApiEndpoints config in Program.cs
             * Despite this, users still need a confirmed email to receive a password reset code when they call the 'forgot password' identity API endpoint (but changing email using the 'manage/info' endpoint works without a confirmed email)
             */
            EmailConfirmed = true;
        }
        
        [InverseProperty(nameof(Quiz.User))]
        public virtual IList<Quiz> Quizzes { get; set; }
    }
}