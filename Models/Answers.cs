using System.ComponentModel.DataAnnotations;
using SimpleMathQuizzesAPI.CustomValidation;

namespace SimpleMathQuizzesAPI.Models
{
    /// <summary>
    /// The answers of a quiz.<br/>
    /// This was made as a separate class to reduce repitition and improve maintainability.
    /// </summary>
    public class Answers
    {
        [Required(ErrorMessage = "A list of answers is required.")]
        [NullOrIntRange(-500, 500, ErrorMessage = "Answers must be null (unanswered), or whole numbers within the range: -500 to 500.")]
        [Display(Name = "User Answers")]
        public IList<int?> UserAnswers { get; set; }
        // needs to be IList (cannot be ICollection), or Index wont work, more details are in the MVC project, in the Index method in QuizController
    }
}
