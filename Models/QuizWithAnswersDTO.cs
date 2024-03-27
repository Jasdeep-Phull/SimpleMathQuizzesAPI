using SimpleMathQuizzesAPI.Entities;

namespace SimpleMathQuizzesAPI.Models
{
    /// <summary>
    /// A quiz, with a list of correct answers for its questions.<br/>
    /// This is used as a DTO for the GetQuizzes(), CreateQuiz() and UpdateQuiz() methods.<br/>
    /// <br/>
    /// The only reason I made this class is because the User property of QuizWithAnswers was being sent with responses.<br/>
    /// Response on frontend from QuizWithAnswers:<br/>
    /// { user: null, (other properties) }<br/>
    /// This class copies all properties from the QuizWithAnswers parameter, except the User.<br/>
    /// <br/>
    /// There is no validation in this class. Objects of this class should only be created/initialised from one of its contructors, and after the Quiz/QuizWithAnswers parameter has been validated against its own data annotation validation.
    /// </summary>
    public class QuizWithAnswersDTO
    {
        public QuizWithAnswersDTO(QuizWithAnswers quizWithAnswers)
        {
            Id = quizWithAnswers.Id;
            CreationDateTime = quizWithAnswers.CreationDateTime;
            Questions = quizWithAnswers.Questions;
            UserAnswers = quizWithAnswers.UserAnswers;
            CorrectAnswers = quizWithAnswers.CorrectAnswers;
            Score = quizWithAnswers.Score;
            UserId = quizWithAnswers.UserId;
        }

        public QuizWithAnswersDTO(Quiz quiz, IList<int> correctAnswers)
        {
            Id = quiz.Id;
            CreationDateTime = quiz.CreationDateTime;
            Questions = quiz.Questions;
            UserAnswers = quiz.UserAnswers;
            CorrectAnswers = correctAnswers;
            Score = quiz.Score;
            UserId = quiz.UserId;
        }

        public int Id { get; set; }
        public DateTimeOffset CreationDateTime { get; set; }
        public IList<string> Questions { get; set; }
        public IList<int?> UserAnswers { get; set; }
        public IList<int> CorrectAnswers { get; set; }
        public int Score { get; set; }
        public string UserId { get; set; }
    }
}
