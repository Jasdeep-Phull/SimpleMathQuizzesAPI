using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzesAPI.Models
{
    /// <summary>
    /// The questions and answers of a quiz.<br/>
    /// This class is also used as a DTO to receive data to create a new quiz.<br/>
    /// This was made as a separate class to reduce repitition and improve maintainability.
    /// </summary>
    public class QuestionsAndAnswers : Answers
    {
        [Required(ErrorMessage = "A list of questions is required.")]
        public IList<string> Questions { get; set; }
        // needs to be IList (cannot be ICollection), or Index wont work, more details are in the MVC project, in the Index method in QuizController
    }
}
