using Microsoft.AspNetCore.Authorization;
using SimpleMathQuizzesAPI.Entities;
using System.Security.Claims;

namespace SimpleMathQuizzesAPI.Services
{
    /// <summary>
    /// Authorization requirement. Only authorized users can access a quiz.<br/>
    /// Currently the only user authorized to access a quiz is the quiz creator.
    /// </summary>
    public class CanAccessQuizRequirement : IAuthorizationRequirement { }

    /// <summary>
    /// Authorization handler for the CanAccessQuiz authorization requirement.<br/>
    /// This is an implementation of resource based authorization in ASP.NET Core, since the quiz is a resource.<br/>
    /// This method retrieves the current user's ID from the authorization context, and checks if the quiz's userId(the quiz creator's user ID) are the same.
    /// </summary>
    public class IsQuizCreatorAuthorizationHandler : AuthorizationHandler<CanAccessQuizRequirement, Quiz>
    {
        protected override Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                CanAccessQuizRequirement requirement,
                Quiz quiz)
        {
            Console.WriteLine("CustomAuthHandlerCalled");
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.Equals(userId, quiz.UserId))
            {
                Console.WriteLine($"u:{userId} == q:{quiz.UserId}, auth success");
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
