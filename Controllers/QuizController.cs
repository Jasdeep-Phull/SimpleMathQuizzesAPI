using System.Data;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SimpleMathQuizzesAPI.Data;
using SimpleMathQuizzesAPI.Entities;
using SimpleMathQuizzesAPI.Models;

namespace SimpleMathQuizzesAPI.Controllers
{
    /// <summary>
    /// The controller responsible for handling all quiz-related requests.<br/>
    /// Has CRUD functions for quizzes, and some helper methods.<br/>
    /// All endpoints in this controller require authentication and return a JSON response.<br/>
    /// (Content negotiation might take place if the request has a different content type in its 'Accept' header, but this has not been tested or verified)
    /// </summary>
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for the QuizController.
        /// </summary>
        /// 
        /// <param name="authorizationService">
        /// The authorization service that only allows authorized users access a quiz.<br/>
        /// The authorization service uses the resource based authorization in QuizCustomAuthorization.cs to determine if the user can access a quiz.
        /// </param>
        /// 
        /// <param name="context">
        /// The EF Core DbContext, which is used to query, create, update and delete data in the Postgresql database of this website.
        /// </param>
        /// 
        /// <param name="logger">
        /// A logger, to log information.
        /// </param>
        public QuizController(IAuthorizationService authorizationService, ApplicationDbContext context, ILogger<QuizController> logger)
        {
            _authorizationService = authorizationService;
            _context = context;
            _logger = logger;
        }


        // GET: api/Quiz
        /// <summary>
        /// Retreives all of the current user's quizzes, by querying the DbContext.<br/>
        /// Calculates the correct answers for each of the current user's quizzes
        /// </summary>
        /// 
        /// <returns>
        /// Returns the current user's quizzes with their correct answers, as a List of QuizWithAnswersDTO objects, as a JSON response (by default)<br/>
        /// Returns a HTTP 401 Unauthorized response if the current user's ID cannot be found in the HttpContext.
        /// </returns>
        [HttpGet]
        public async Task<ActionResult<IList<QuizWithAnswersDTO>>> GetQuizzes()
        {
            _logger.LogInformation("\nIndex Called");

            string? userId = GetCurrentUserId();
            if (userId is null)
            {
                _logger.LogInformation("Unauthenticated user has requested to view quizzes (Index Page))");
                return Unauthorized();
            }

            // try to find the quiz in the DbContext
            IList<Quiz> userQuizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.UserId == userId)
                .ToListAsync();

            // create a list of QuizWithAnswersDTO objects from the user's quizzes
            List<QuizWithAnswersDTO> userQuizzesWithAnswers = userQuizzes
                .Select(quiz =>
                    new QuizWithAnswersDTO(quiz, CalculateAnswers(quiz.Questions)))
                .ToList();


            return userQuizzesWithAnswers;
            // the front end calculates the score percentages for each quiz
        }


        /* The methods within this comment are not currently being used/called by the front end
        
        // GET: api/Quiz/5/
        /// <summary>
        /// Queries the context for a quiz.
        /// Uses the GetQuizHelper method to avoid code repitition
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns the quiz, as a JSON response (by default)<br/>
        /// Returns a HTTP 404 Not Found result if the quiz cannot be found.<br/>
        /// Returns a HTTP 403 Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// Returns a HTTP 500 Internal Server Error Problem Details response if GetQuizHelper did not return a QuizWithAnswers object or an Action Result
        /// </returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Quiz>> GetQuiz(int id)
        {
            var returnValue = await GetQuizHelper(id);

            // switch statements are better than if-else statements, but using a switch statement to check for object types is needlessly complicated
            if (returnValue is Quiz quiz)
            {
                return quiz;
            }
            else if (returnValue is ActionResult actionResult)
            {
                return actionResult;
            }
            else
            {
                _logger.LogInformation("return value from GetQuizHelper was not a QuizWithAnswers object or an action result\nreturnValue: {returnValue}", returnValue);

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: "Unable to retrieve quiz: Unexpected error encountered when retrieving quiz");
            }
        }

        // GET: api/Quiz/5/true
        // (need to be separate methods because they have different return types)
        /// <summary>
        /// Queries the context for a quiz.
        /// Uses the GetQuizHelper method to avoid code repitition
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns the quiz and its answers as a QuizWithAnswersDTO object, as a JSON response (by default)<br/>
        /// Returns a HTTP 404 Not Found result if the quiz cannot be found.<br/>
        /// Returns a HTTP 403 Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// Returns a HTTP 500 Internal Server Error Problem Details response if GetQuizHelper did not return a QuizWithAnswers object or an Action Result
        /// </returns>
        [HttpGet("{id}/true")]
        public async Task<ActionResult<QuizWithAnswersDTO>> GetQuizWithAnswers(int id)
        {
            object returnValue = await GetQuizHelper(id, true);

            // switch statements are better than if-else statements, but using a switch statement to check for object types is needlessly complicated
            if (returnValue is QuizWithAnswers quizWithAnswers)
            {
                return new QuizWithAnswersDTO(quizWithAnswers); // remove the User navigational property before sending this as a response
                // the User navigational property is null because it was not included in the EF core query
            }
            else if (returnValue is ActionResult actionResult)
            {
                return actionResult;
            }
            else
            {
                _logger.LogInformation("return value from GetQuizHelper was not a QuizWithAnswers object or an action result\nreturnValue: {returnValue}", returnValue);

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: "Unable to retrieve quiz with answers: Unexpected error encountered when retrieving quiz with answers");
            }
        }

        /// <summary>
        /// Queries the context for a quiz.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <param name="withAnswers">
        /// Determines what to return:<br/>
        /// if this parameter is "true", then a QuizWithAnswers object is returned, which includes the correct answers for each question in the quiz.<br/>
        /// if this parameter is "false", then the quiz is returned without its correct answers.
        /// </param>
        /// 
        /// <returns>
        /// Returns the quiz and its answers as a QuizWithAnswers object, if the parameter 'withAnswers' is true.<br/>
        /// Returns the quiz, if the parameter 'withAnswers' false.<br/>
        /// Returns a HTTP 404 Not Found result if the quiz cannot be found.<br/>
        /// Returns a HTTP 403 Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        private async Task<object> GetQuizHelper(int id, bool withAnswers = false)
        {
            // try to find the quiz in the DbContext
            Quiz? quiz = await _context.Quizzes.FindAsync(id); // the User navigational property has not been included in this query

            if (quiz is null) { return NotFound(); }
            _logger.LogInformation("Quiz found");

            _logger.LogInformation($"userId {HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)}");
            _logger.LogInformation($"Quiz UserId {quiz.UserId}");
            string? userId = GetCurrentUserId();

            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (authorizationResult.Succeeded)
            {
                _logger.LogInformation("Authorised user (ID: {userId}) has requested to view a quiz (ID: {id})", userId, id);

                // return based on the value of the withAnswers parameter
                if (!withAnswers)
                {
                    return quiz;
                }
                else
                {
                    IList<int> correctAnswers = CalculateAnswers(quiz.Questions);
                    return new QuizWithAnswers(quiz, correctAnswers);
                }
            }
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Unauthorised user (ID: {userId}) has requested to view a quiz (ID: {id})", userId, id);
                return new ForbidResult();
            }
            else
            {
                _logger.LogInformation("Unauthenticated user has requested to view quiz (ID: {id})", id);
                return new ChallengeResult();
            }
        }
        */


        // POST: api/Quiz
        /// <summary>
        /// Validates the create data.<br/>
        /// Creates a new quiz, calculates the score of the quiz and populates all other quiz fields.<br/>
        /// Saves the changes to the DbContext.
        /// </summary>
        /// 
        /// <param name="createData">
        /// The questions and answers to create a quiz from.<br/>
        /// this data is received as a QuestionsAndAnswers object through Model Binding.
        /// </param>
        /// 
        /// <returns>
        /// Returns a HTTP 201 JSON response if successful. This response contains the newly created quiz, as a QuizWIthAnswersDTO object, as a JSON object response (by default)<br/>
        /// Returns a Problem Details JSON response with status code greater than or equal to 400 if any problems or errors are encountered during the request.
        /// </returns>
        [HttpPost]
        public async Task<ActionResult<QuizWithAnswersDTO>> CreateQuiz([FromBody] QuestionsAndAnswers createData)
        {
            _logger.LogInformation("\nCreate Post Called");

            // check if request body was null
            if (createData is null)
            {
                string errorMessage = "Unable to create quiz: data for new quiz deserialised from JSON request was null";
                _logger.LogError(errorMessage);

                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: errorMessage);
            }


            // check if data is valid
            if (!TryValidateModel(createData))
            {
                string errorMessage = "Unable to create quiz: data for new quiz was invalid";
                _logger.LogInformation(errorMessage);

                LogError(ModelState);

                return ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: errorMessage,
                    modelStateDictionary: ModelState);
            }
            _logger.LogInformation("CreateData is valid");


            // try to calculate the score of the quiz
            int score = -1;
            IList<int> correctAnswers = [];
            try
            {
                correctAnswers = CalculateAnswers(createData.Questions); // if this request is successful, the answers calculated here will be returned with the quiz
                score = CalculateScore(createData.Questions, createData.UserAnswers, correctAnswers);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when calculating the score and correct answers of the quiz";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: errorMessage);
            }
            _logger.LogInformation("Score calculated successfully");


            string? userId = GetCurrentUserId();
            // check if userId is null (couldn't find the current user's Id in the HttpContext)
            if (userId is null)
            {
                string errorMessage = "Unable to create quiz: unable to retrieve the current user's ID";
                _logger.LogError(errorMessage);
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: errorMessage);
            }
            _logger.LogInformation("UserId successfully determined");

            // find user in the DbContext
            User? user = await _context.Users
                    .Include(u => u.Quizzes)
                    .FirstOrDefaultAsync(u => u.Id == userId);

            // check if user is null (couldn't find the current user's information in the database, using their Id)
            if (user is null)
            {
                string errorMessage = "Unable to create quiz: unable to retrieve the current user's data using the current user's ID";
                _logger.LogError(errorMessage);
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage);
            }
            _logger.LogInformation("User successfully retrieved from ID");


            // create a new quiz object
            Quiz quiz = new()
            {
                // quiz ID is automatically generated
                CreationDateTime = DateTimeOffset.Now, // the Postgresql database uses the "timestamp with time zone" datatype for CreationDateTime, so DateTimeOffset is needed
                Questions = createData.Questions,
                UserAnswers = createData.UserAnswers,
                Score = score,
                UserId = userId,
                User = user // the method "user.Quizzes.Add(quiz)" below might auto assign this, but i manually assign it just in case it doesn't
            };


            /* This code checks if user.Quizzes is null (has not been initialised), because trying to add to a null list will cause an exception
             * This is not needed anymore, because user.Quizzes is initialised as an empty list in the User constructor
             * This is left here in case it might be useful
            
            _logger.LogInformation($"User quizzes: {string.Join(", ", user.Quizzes)}");
            if (user.Quizzes is null)
            {
                _logger.LogInformation($"Quizzes is null");
                user.Quizzes = [];
            }
            else if (user.Quizzes.Count == 0)
            {
                _logger.LogInformation($"Quizzes is empty");
            }
            else
            {
                _logger.LogInformation($"Number of user quizzes: {user.Quizzes.Count}");
            }

            */


            /* adding the quiz to the user's quizzes before validating the quiz
             * this isn't a problem because "_context.SaveChangesAsync()" is called only if the quiz is valid
             * so if the quiz was not valid then this change wouldn't be saved
             */
            user.Quizzes.Add(quiz);


            _logger.LogInformation("Quiz created");

            /* logs for all fields if needed
            
            id is 0 here, but will be correctly generated when _context.SaveChangesAsync() is successful
            _logger.LogInformation($"CreationDateTime: {quiz.CreationDateTime}");
            _logger.LogInformation($"Questions: {string.Join(", ", quiz.Questions)}");
            _logger.LogInformation($"Answers: {string.Join(", ", quiz.UserAnswers)}");
            _logger.LogInformation($"Score: {quiz.Score}");
            _logger.LogInformation($"UserId: {quiz.UserId}");
            _logger.LogInformation($"User: {quiz.User.Id}, {quiz.User}");
            */


            /* check if newly created quiz is valid
             * this is mainly to check that the automatically populated fields (Score, CreationDateTime, UserId and User) are valid
             * the questions and answers are already valid, because they passed the 1st validation check in this method
             */
            if (!TryValidateModel(quiz))
            {
                string errorMessage = "Unable to create quiz: unexpected error encountered when creating quiz and populating automatically computed fields";
                _logger.LogError(errorMessage);

                LogError(ModelState);

                /* return a HTTP 500 response for this validation error, since the fields that fail validation here were automatically populated by the controller, not the user
                 * if Questions or UserAnswers were invalid, they would fail the 1st validation check in this method
                 */
                return ValidationProblem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage,
                    modelStateDictionary: ModelState);
            }
            _logger.LogInformation("Quiz is valid");


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to create quiz: unexpected error encountered when saving new quiz to database";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);
                
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage);
            }
            // if "saveChangesAsync()" did not throw an exception, then it was executed successfully


            string successMessage = "Successfully created new quiz (and updated database)";
            _logger.LogInformation(successMessage);

            return CreatedAtAction(
                actionName: null,
                value: new QuizWithAnswersDTO(quiz, correctAnswers)); // the quiz's Id is automatically populated after calling SaveChangesAsync()
        }


        // PATCH: api/Quiz/5
        /// <summary>
        /// Validates the update data.<br/>
        /// Re-calculates the score of the quiz.<br/>
        /// Saves the changes to the DbContext.
        /// </summary>
        /// 
        /// <param name="updateData">
        /// The id of the quiz, to find it in the DbContext, and the new answers to update the quiz with.<br/>
        /// This data is received as a ReceiveUpdatedUserAnswersModel object through Model Binding.
        /// </param>
        /// 
        /// <returns>
        /// Returns a HTTP 200 JSON response if successful.<br/>
        /// Returns a Problem Details JSON response with status code greater than or equal to 400 if any problems or errors are encountered during the request.
        /// </returns>
        [HttpPatch("{id}")]
        // public async Task<ActionResult<QuizWithAnswersDTO>> UpdateQuiz(int id, [FromBody] Answers updateData)
        public async Task<ActionResult<QuizWithAnswersDTO>> UpdateQuiz([FromBody] ReceiveUpdatedUserAnswersModel updateData)
        {
            _logger.LogInformation("Edit Post Called");

            // check if request body is null
            if (updateData is null)
            {
                string errorMessage = "Unable to update quiz: id and answers deserialised from JSON request were null";
                _logger.LogError(errorMessage);
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: errorMessage);
            }
            _logger.LogInformation("updateData is not null");


            // try to find the quiz in the DbContext, using the id in updateData
            /* (Finding the quiz before checking if update data is valid because it is more efficient
             * there is no point validating the data first if the quiz to update cannot be found)
             */
            Quiz? quiz = await _context.Quizzes
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == updateData.Id);

            // check if quiz is null (couldn't find the quiz in the context)
            if (quiz is null)
            {
                string errorMessage = "Unable to update quiz: quiz to update cannot be found";
                _logger.LogInformation(errorMessage);
                // return NotFound();
                return Problem(
                    statusCode: 404,
                    detail: errorMessage);
            }
            _logger.LogInformation($"quiz to update found (ID: {updateData.Id})");
            _logger.LogInformation($"quiz userId: {quiz.User.Id})"); // using .User.Id instead of .userId to check that the navigation property has been loaded;


            // check if updateData is valid
            if (!TryValidateModel(updateData))
            {
                string errorMessage = "Unable to update quiz: answers were invalid";
                _logger.LogInformation(errorMessage);

                LogError(ModelState);

                return ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: errorMessage,
                    modelStateDictionary: ModelState);
            }
            _logger.LogInformation("updateData is valid");

            string? userId = GetCurrentUserId();
            // authorization handler deals with userId == null

            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (!authorizationResult.Succeeded)
            {
                if (HttpContext.User.Identity?.IsAuthenticated == true)
                /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
                 * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a cast error
                 */
                {
                    _logger.LogInformation("Unauthorised user (ID: {userId}) has posted an edit request for a quiz (ID: {quiz.Id})", userId, quiz.Id);
                }
                else
                {
                    _logger.LogInformation("Unauthenticated user has requested access to edit quiz (ID: {quiz.Id})", quiz.Id);
                }
                return Forbid();
            }
            _logger.LogInformation("Authorised user (ID: {userId}) has posted an edit request for a quiz (ID: {quiz.Id})", userId, quiz.Id);


            // try to re-calculate the score of the quiz
            int score = -1;
            IList<int> correctAnswers = [];
            try
            {
                correctAnswers = CalculateAnswers(quiz.Questions);
                score = CalculateScore(quiz.Questions, updateData.UserAnswers, correctAnswers);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when re-calculating the score of the quiz";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage);
            }
            _logger.LogInformation("score re-calculated successfully");


            /* update quiz user answers and score
             * saveChangesAsync() has not been called yet, so these changes will not be saved yet if there are any problems
             */
            quiz.Score = score;
            quiz.UserAnswers = updateData.UserAnswers;
            _logger.LogInformation("updated data assigned");


            /* check if the quiz is still valid after the changes have been made
             * this is mainly to check that the score calculated is valid
             * the new answers are already valid, because they passed the 1st validation check in this method
             */
            if (!TryValidateModel(quiz))
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when updating the quiz";
                _logger.LogError(errorMessage);

                LogError(ModelState);

                /* return a HTTP 500 response for this validation error, since the fields that fail validation here were automatically populated by the controller, not the user
                 * if the new answers were invalid, they would fail the 1st validation check in this method
                 */
                return ValidationProblem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage,
                    modelStateDictionary: ModelState);
            }
            _logger.LogInformation("quiz is valid");


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!QuizExists(quiz.Id))
                {
                    // quiz no longer exists in the database
                    string errorMessage = "Unable to update quiz: quiz to update does not exist";
                    _logger.LogInformation(errorMessage);

                    // return NotFound();
                    return Problem(
                        statusCode: 404,
                        detail: errorMessage);
                }
                else
                {
                    _logger.LogError("Unable to update quiz: DbUpdateConcurrencyException : {ex}", ex);
                    
                    throw;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to update quiz: unexpected error encountered when saving changes to database";
                _logger.LogError(errorMessage);
                _logger.LogInformation("Exception: {ex}", ex);

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: errorMessage);
            }
            // if "saveChangesAsync()" did not throw an exception, then it was executed successfully


            string successMessage = $"Succcessfully updated quiz (ID: {quiz.Id}) (and updated database)";
            _logger.LogInformation(successMessage);

            return new OkObjectResult(new QuizWithAnswersDTO(quiz, correctAnswers));
            // normally a HTTP 204 No Content response is returned for an update request, but the front end needs to update its store with the new score of the quiz, so this method returns an OkObjectResult with the updated quiz
        }


        // DELETE: api/Quiz/5
        /// <summary>
        /// Queries the context for the quiz.<br/>
        /// Removes the quiz from the DbContext.<br/>
        /// Saves the changes to the DbContext.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns a HTTP 204 No Content response if the delete request was successful.<br/>
        /// Returns a HTTP 404 NotFound result if the quiz cannot be found.<br/>
        /// Returns a HTTP 500 Internal Server Error Problem Details JSON response if the changes could not be saved to the DbContext.<br/>
        /// Returns a HTTP 403 Forbid result if the user is unauthorized to access the quiz.<br/>
        /// Returns a Challenge result if the user is unauthenticated.
        /// </returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            _logger.LogInformation("Delete Post Called");

            // try to find the quiz in the context
            Quiz? quiz = await _context.Quizzes.FindAsync(id);
            if (quiz is null) { return NotFound(); }

            string? userId = GetCurrentUserId();
            // authorization handler deals with userId == null

            // check if the current user is authorized to access this quiz
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, quiz, "CanAccessQuiz");
            if (authorizationResult.Succeeded)
            {
                _logger.LogInformation("Authorised user (ID: {userId}) has posted a request to delete a quiz (ID: {id})", userId, id);
                // if the user has been authenticated then the id will not be null

                // find the user in the DbContext
                // if the user has been authenticated then the user returned from this query will not be null
                User? user = await _context.Users
                    .Include(u => u.Quizzes)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // remove this quiz from the user's quizzes
                user.Quizzes.Remove(quiz);

                /* also directly remove the quiz from the context
                 * the above line of code should automatically do this, but i left this here just in case it doesn't
                 */
                _context.Quizzes.Remove(quiz);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    string errorMessage = "Unable to delete quiz: unexpected error encountered when saving changes to the database";
                    _logger.LogError(errorMessage);
                    _logger.LogInformation("Exception: {ex}", ex);
                    return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        detail: errorMessage);
                }
                // if "saveChangesAsync()" did not throw an exception, then it was executed successfully

                _logger.LogInformation("Successfully deleted quiz (ID: {id})", id);
                return NoContent();
            }
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            /* "== true" is needed in the above statement. if it isn't there then the compiler will throw CS0266: cannot convert bool? to bool
             * this can be solved by casting to bool, but casting makes things unnecessarily messy, because then you need a "catch" block to catch a caste error
             */
            {
                _logger.LogInformation("Unauthorised user (ID: {userId}) has posted a request to delete a quiz (ID: {id})", userId, id);
                return new ForbidResult();
            }
            else
            {
                _logger.LogInformation("Unauthenticated user has posted a request to delete quiz (ID: {id})", id);
                return new ChallengeResult();
            }
        }


        // GET: api/Quiz/Questions/10
        /// <summary>
        /// Generates a list of questions for a new quiz.<br/>
        /// Also tries to calculate the answer for each question before adding it the the list of questions.<br/>
        /// This avoids the possibility/problem of passing a question that cannot be evaluated to the user.<br/>
        /// <br/>
        /// Current range of values for each question:<br/>
        /// [1-100] + [1-100]<br/>
        /// [10-100] - [1-99]<br/>
        /// [2-10] * [2-20]
        /// </summary>
        /// 
        /// <param name="numberOfQuestions">
        /// The number of questions to generate.
        /// </param>
        /// 
        /// <returns>
        /// Returns the generated questions, as a List of Strings, as a JSON response (by default)<br/>
        /// Returns a HTTP 400 response if the parameter numberOfQuestions is equal to or below 0.<br/>
        /// Returns a HTTP 501 Not Implemented Problem Details JSON response if the parameter numberOfQuestions is greater than 30.
        /// </returns>
        [HttpGet("Questions/{numberOfQuestions}")]
        public ActionResult<IList<string>> GenerateQuestions(int numberOfQuestions)
        {
            switch (numberOfQuestions)
            {
                case <= 0:
                    return BadRequest();
                case > 30:
                    return Problem(
                        statusCode: StatusCodes.Status501NotImplemented,
                        detail: "Generating and returning a large amount of questions is not recommended, because it has not been tested");
                default:
                    break;
            };
            
            /*
            Range of values for each question:
                [1<->100] + [1<->100]
                [10<->100] - [1<->99]
                [2<->10] * [2<->20]
            */

            List<string> operators = ["+", "-", "*"];

            // limits for each value and operator
            const int ADD_MIN_VALUE = 1;
            const int ADD_MAX_VALUE = 100;

            const int SUBTRACT_MIN_FIRST_VALUE = 10;
            const int SUBTRACT_MAX_FIRST_VALUE = 100;
            const int SUBTRACT_MIN_SECOND_VALUE = 1;
            const int SUBTRACT_MAX_SECOND_VALUE = 99;

            const int MULTIPLY_MIN_VALUE = 2;
            const int MULTIPLY_MAX_FIRST_VALUE = 10;
            const int MULTIPLY_MAX_SECOND_VALUE = 20;

            Random rand = new();

            IList<string> questions = [];
            for (int i = 0; i < numberOfQuestions; i++)
            {
                string question = "";

                bool unique = false;
                while (unique is false)
                {
                    // generate question
                    int operatorIndex = rand.Next(operators.Count);
                    string questionOperator = operators[operatorIndex];
                    int firstNumber, secondNumber;

                    switch (questionOperator)
                    {
                        case "+":
                            firstNumber = rand.Next(ADD_MIN_VALUE, ADD_MAX_VALUE);
                            secondNumber = rand.Next(ADD_MIN_VALUE, ADD_MAX_VALUE);
                            break;
                        case "-":
                            firstNumber = rand.Next(SUBTRACT_MIN_FIRST_VALUE, SUBTRACT_MAX_FIRST_VALUE);
                            secondNumber = rand.Next(SUBTRACT_MIN_SECOND_VALUE, SUBTRACT_MAX_SECOND_VALUE);
                            break;
                        case "*":
                            firstNumber = rand.Next(MULTIPLY_MIN_VALUE, MULTIPLY_MAX_FIRST_VALUE);
                            secondNumber = rand.Next(MULTIPLY_MIN_VALUE, MULTIPLY_MAX_SECOND_VALUE);
                            break;
                        default:
                            _logger.LogError("Question operator ({questionOperator}) is not +, - or *", questionOperator);
                            continue;
                    }

                    question = firstNumber.ToString() + questionOperator + secondNumber.ToString();

                    // check if this question has already been generated, and start a new iteration if it has
                    if (questions.Contains(question)) { continue; }

                    /* check if question can be evaluated before it is sent to the view
                     * EvaluateQuestion() is called with throwException = false, because there is no need to terminate the program if there is an error here. Instead the question will be re-generated, and the error will be logged inside EvaluateQuestion()
                     */
                    int? answer = EvaluateQuestionNullable(question, false);

                    // check if the question was successfully evaluated, and start a new iteration if it wasn't
                    if (answer is null) { continue; }
                    // EvaluateQuestionNullable() handles logging for questions that cannot be evaluated
                    _logger.LogInformation($"{question}, answer: {answer}");

                    unique = true;
                }
                questions.Add(question);
            }

            string questionsString = string.Join(", ", questions);
            _logger.LogInformation("List of questions returned from GenerateQuestions({numberOfQuestions}): {questionsString}", numberOfQuestions, questionsString);

            return (List<string>)questions;
            /* Removing the cast will cause in the following error:
             * CS0029: Cannot implicitly convert type 'System.Collections.Generic.IList<string>' to 'Microsoft.AspNetCore.Mvc.ActionResult<System.Collections.Generic.IList<string>>'
             * 
             * For this case, casting to List<string> seems better than using questions.ToList(), but questions.ToList() is an alternative solution that will compile if preferred (but it hasn't been tested during runtime)
             */
        }



        // non endpoint methods (private helper methods):



        /// <summary>
        /// Checks if the quiz exists in the DbContext.
        /// </summary>
        /// 
        /// <param name="id">
        /// The id of the quiz to look for.
        /// </param>
        /// 
        /// <returns>
        /// Returns True if the quiz is in the DbContext.<br/>
        /// Returns False if the quiz cannot be found in the DbContext.
        /// </returns>
        private bool QuizExists(int id)
        {
            return _context.Quizzes.Any(e => e.Id == id);
        }

        /// <summary>
        /// Retrieves the current user's ID from the claims stored in the HttpContext.User ClaimsPrincipal.
        /// </summary>
        /// 
        /// <returns>
        /// Returns the current user's ID.<br/>
        /// Returns null if the current user's ID cannot be found in the HttpContext.User ClaimsPrincipal.
        /// </returns>
        private string? GetCurrentUserId()
        {
            string? id = null;

            if (HttpContext.User is null) { _logger.LogError("Current user is unauthenticated"); }
            else
            {
                // "By default, the JWT authentication handler in .NET will map the sub claim of a JWT access token to the System.Security.Claims.ClaimTypes.NameIdentifier claim type"
                // https://stackoverflow.com/questions/46112258/how-do-i-get-current-user-in-net-core-web-api-from-jwt-token
                id = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (id is null) { _logger.LogError("Current user is unauthenticated"); }
            }
            return id;
        }


        /// <summary>
        /// Logs all of the errors in the modelState parameter as one string, separated by new line characters ("\n").
        /// </summary>
        /// 
        /// <param name="modelState">
        /// The ModelStateDictionary to check for errors.
        /// </param>
        private void LogError(ModelStateDictionary modelState)
        {
            _logger.LogInformation($"Number of errors: {modelState.ErrorCount}");

            IEnumerable<string?> modelErrors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(v => v.ErrorMessage);

            string errors = string.Join(", ", modelErrors);
            _logger.LogInformation("Errors: {errors}", errors);
        }


        /// <summary>
        /// Calculates the score of a quiz.<br/>
        /// This is done by comparing the correct answer of each question with the corressponding user answer, and incrementing the score if they are the same
        /// </summary>
        /// 
        /// <param name="questions">
        /// The questions of the quiz
        /// </param>
        /// 
        /// <param name="userAnswers">
        /// The user answers of the quiz
        /// </param>
        /// 
        /// <returns>
        /// The score of the quiz, as an Integer
        /// </returns>
        private int CalculateScore(IList<string> questions, IList<int?> userAnswers)
        {
            return CalculateScore(questions, userAnswers, CalculateAnswers(questions));
        }


        /// <summary>
        /// Calculates the score of a quiz.<br/>
        /// This is done by comparing the correct answer of each question with the corressponding user answer, and incrementing the score it they are the same
        /// </summary>
        /// 
        /// <param name="questions">
        /// The questions of the quiz
        /// </param>
        /// 
        /// <param name="userAnswers">
        /// The user answers of the quiz
        /// </param>
        /// 
        /// <param name="userAnswers">
        /// The correct answers of the quiz
        /// </param>
        /// 
        /// <returns>
        /// The score of the quiz, as an Integer
        /// </returns>
        private int CalculateScore(IList<string> questions, IList<int?> userAnswers, IList<int> correctAnswers)
        {
            int score = 0;

            for (int i = 0; i < questions.Count; i++)
            {
                int? userAnswer = userAnswers.ElementAt(i);
                // int? userAnswer = userAnswers.ElementAtOrDefault(i);
                int correctAnswer = correctAnswers.ElementAt(i);

                if (userAnswer == correctAnswer)
                {
                    score++;
                    _logger.LogInformation("UserAnswer: {userAnswer}, CorrectAnswer: {correctAnswer}. Answer matches, score incremented", userAnswer, correctAnswer);
                }
                else
                {
                    _logger.LogInformation("UserAnswer: {userAnswer}, CorrectAnswer: {correctAnswer}. Answer does not match, score not incremented", userAnswer, correctAnswer);
                }
            }
            _logger.LogInformation("Score returned: {score}", score);
            return score;
        }


        /// <summary>
        /// Calculates the correct answers for a list of questions.
        /// </summary>
        /// 
        /// <param name="questions">
        /// The questions to calculate answers for
        /// </param>
        /// 
        /// <returns>
        /// The correct answers for the questions supplied, as a List of Integers
        /// </returns>
        private IList<int> CalculateAnswers(IList<string> questions)
        {
            return questions
                .Select(EvaluateQuestion)
                .ToList();
        }


        /// <summary>
        /// Calculates the answer to the question supplied, by using the DataTable.Compute() method.<br/>
        /// This method will throw any exceptions it encounters.<br/>
        /// This method calls EvaluateQuestionNullable() with parameter throwException = true, and then casts the result from int? to int
        /// </summary>
        /// 
        /// <param name="question">
        /// The question to calculate an answer for.
        /// </param>
        /// 
        /// <returns>
        /// The correct answer for the question, as an Integer
        /// </returns>
        /// 
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the result of EvaluateQuestionNullable(question, true) cannot be cast to int.<br/>
        /// As far as I can see, this can only happen if EvaluteQuestionNullable(question, true) returns null (despite being called with throwException = true)
        /// </exception>
        /// 
        /// <exception cref="Exception">
        /// This exception is thrown if any error except InvalidOperationException is encountered when casting from int? to int.<br/>
        /// (This is not thrown if there are errors from EvaluateQuestionNullable(), because EvaluateQuestionNullable() handles its own errors/exceptions)
        /// </exception>
        private int EvaluateQuestion(string question)
        {
            try
            {
                int answer = (int)EvaluateQuestionNullable(question, true);
                return answer;
            }
            catch (InvalidOperationException ex)
            {
                string errorMessage = $"EvaluteQuestionNullable({question}, true) returned null";
                _logger.LogCritical(errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unknown error when casting int? to int: {question}";
                _logger.LogCritical(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }
        

        /// <summary>
        /// Calculates the answer to the question supplied, by using the DataTable.Compute() method.<br/>
        /// The throwException parameter determines how this method deals with errors/exceptions.<br/>
        /// <br/>
        /// Having a version of this method that does not throw exceptions is useful when pre-evaluating generated questions. Please check the comments in generateQuestions() for more details.
        /// </summary>
        /// 
        /// <param name="question">
        /// The question to calculate an answer for.
        /// </param>
        /// 
        /// <param name="throwException">
        /// Determines what to do when errors are encountered.<br/>
        /// if this is "True" then any exceptions encountered will be thrown.<br/>
        /// if this is "False" then any exceptions encountered will be suppressed.
        /// </param>
        /// 
        /// <returns>
        /// Returns the correct answer for the question, as an Integer.<br/>
        /// Returns null if any errors were encountered and throwException = false.
        /// </returns>
        private int? EvaluateQuestionNullable(string question, bool throwException = true)
        {
            // the value to return if throwException is false
            int? notThrownExceptionReturnValue = null;

            try
            {
                int correctAnswer = Convert.ToInt32(new DataTable().Compute(question, null));
                return correctAnswer;
            }
            catch (ArgumentNullException ex) // question was null
            {
                int? returnValue = (int?)HandleException(
                    $"Question is null",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (EvaluateException ex) // the Datatable could not compute the question
            {
                int? returnValue = (int?)HandleException(
                    $"Unable to evaluate question: {question}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (FormatException ex) // The result of DataTable.Compute() could not be converted to an Integer
            {
                int? returnValue = (int?)HandleException(
                    $"Computed answer cannot be converted to int: {new DataTable().Compute(question, null)}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;
            }
            catch (Exception ex) // an unknown exception occured when evaluating the question
            {
                int? returnValue = (int?)HandleException(
                    $"Unknown error when evaluating question: {question}",
                    ExceptionDispatchInfo.Capture(ex),
                    throwException,
                    notThrownExceptionReturnValue);
                return returnValue;

            }
        }


        /// <summary>
        /// If the parameter throwException = true, then throw the exception = true, then throw the exception stored in the ExceptionDispatchInfo.<br/>
        /// If the parameter throwException = false, return the parameter returnValue.<br/>
        /// <br/>
        /// This method's main purpose is to avoid code repitition.
        /// </summary>
        /// 
        /// <param name="errorMessage">
        /// The error message to log
        /// </param>
        /// 
        /// <param name="ex">
        /// The ExceptionDispatchInfo object, with the exception stored inside it
        /// </param>
        /// 
        /// <param name="throwException">
        /// Determines what to do when errors are encountered.<br/>
        /// if this is "True" then any exceptions encountered will be thrown.<br/>
        /// if this is "False" then any exceptions encountered will be suppressed.
        /// </param>
        /// 
        /// <param name="returnValue">
        /// The value to return if throwException = false.
        /// </param>
        /// 
        /// <returns>
        /// Throws the exception inside the parameter 'ex' if the parameter 'throwException' is true.<br/>
        /// Returns the parameter 'returnValue' if the parameter 'throwException' is false.
        /// </returns>
        private object? HandleException(string errorMessage, ExceptionDispatchInfo ex, bool throwException, object? returnValue)
        {
            _logger.LogError(errorMessage);
            _logger.LogInformation("Exception: {ex.SourceException}", ex.SourceException);
            // _logger.LogInformation("Stack Trace: {ex.SourceException.StackTrace}");
            // since the exception is logged, the stack trace should have already been logged
            if (throwException)
            {
                ex.Throw();
            }
            // else
            return returnValue;
        }
    }
}
