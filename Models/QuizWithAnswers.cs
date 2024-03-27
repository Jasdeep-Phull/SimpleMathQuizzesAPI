using SimpleMathQuizzesAPI.Entities;
using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzesAPI.Models
{
    /// <summary>
    /// A quiz, with a list of correct answers for its questions
    /// </summary>
    public class QuizWithAnswers : Quiz
    {
        public QuizWithAnswers(Quiz quiz, IList<int> correctAnswers)
        {
            Id = quiz.Id;
            CreationDateTime = quiz.CreationDateTime;
            Questions = quiz.Questions;
            UserAnswers = quiz.UserAnswers;
            CorrectAnswers = correctAnswers;
            Score = quiz.Score;
            UserId = quiz.UserId;
            // omitting User navigational property
        }

        [Required(ErrorMessage = "The list of correct answers was not automatically populated")]
        public IList<int> CorrectAnswers { get; set; }
    }
}
