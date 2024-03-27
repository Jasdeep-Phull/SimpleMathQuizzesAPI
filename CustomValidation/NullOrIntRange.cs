using System.ComponentModel.DataAnnotations;

namespace SimpleMathQuizzesAPI.CustomValidation
{
    /// <summary>
    /// Custom validation to check if all answers for a quiz are either:<br/>
    /// - null (unanswered), or<br/>
    /// - above the minimum value, and below the maximum value (both are provided as parameters)
    /// </summary>
    public sealed class NullOrIntRange : ValidationAttribute
    {
        private readonly int _minimumValue;
        private readonly int _maximumValue;

        /// <summary>
		/// Constructor for this custom validation.<br/>
        /// the minimum and maximum values are inclusive.<br/>
        /// e.g. if maximumValue = 500, then 500 is valid.
        /// </summary>
        /// 
        /// <param name="minimumValue">
        /// the minimum value for an answer
        /// </param>
        /// 
        /// <param name="maximumValue">
        /// the maxiumum value for an answer
        /// </param>
        public NullOrIntRange(int minimumValue, int maximumValue)
        {
            _minimumValue = minimumValue;
            _maximumValue = maximumValue;
        }

        public override bool IsValid(object? value)
        {
            // while all individual answers can be null, the entire answers list cannot be null
            if (value is null) { return false; }

            List<int?> answers = [];
            try
            {
                answers = (List<int?>)value;
            }
            catch { return false; }


            foreach (int? answer in answers)
            {
                if (answer is null) { continue; }

                if (answer < _minimumValue || answer > _maximumValue) { return false; }
            }

            // if the code has reached this point, then all answers are valid
            return true;
        }
    }
}
