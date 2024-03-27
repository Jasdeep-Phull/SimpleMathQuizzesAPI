using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzesAPI.Models
{
    /// <summary>
    /// The updated answers of a quiz and its ID.<br/>
    /// This is used as a DTO for editing the user answers of a quiz.
    /// </summary>
    public class ReceiveUpdatedUserAnswersModel : Answers
    {
        [Required(ErrorMessage = "A quiz ID is required for an update request.")]
        public int Id { get; set; }
    }
}