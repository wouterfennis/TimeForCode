using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    public class UserController : ControllerBase
    {
        private readonly IAccountInformationRepository _accountInformationRepository;
        private readonly IGithubApiService _githubApiService;
        private readonly IEncryptionService _encryptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        public UserController(
            IAccountInformationRepository accountInformationRepository,
            IGithubApiService githubApiService,
            IEncryptionService encryptionService)
        {
            _accountInformationRepository = accountInformationRepository;
            _githubApiService = githubApiService;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Get user information.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserAsync()
        {
            var userId = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!;
            var userEntity = await _accountInformationRepository.GetByInternalIdAsync(userId);

            if (userEntity == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "User not found",
                    Detail = $"User with id {userId} not found"
                };

                return NotFound(problemDetails);
            }

            var user = new UserResponse
            {
                Id = userEntity.Id.ToString(),
                Name = userEntity.Name,
                Email = userEntity.Email,
                Login = userEntity.Login,
                NodeId = userEntity.NodeId,
                AvatarUrl = userEntity.AvatarUrl,
                Company = userEntity.Company,
                Bio = userEntity.Bio,
                Location = userEntity.Location
            };

            return Ok(user);
        }

        /// <summary>
        /// Get user's public repositories from GitHub.
        /// </summary>
        /// <returns></returns>
        [HttpGet("repositories")]
        [ProducesResponseType(typeof(IEnumerable<RepositoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserRepositoriesAsync()
        {
            var userId = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!;
            var userEntity = await _accountInformationRepository.GetByInternalIdAsync(userId);

            if (userEntity == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "User not found",
                    Detail = $"User with id {userId} not found"
                };

                return NotFound(problemDetails);
            }

            if (string.IsNullOrEmpty(userEntity.EncryptedGitHubAccessToken))
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "No GitHub access token available. Please re-authenticate via GitHub."
                };

                return Unauthorized(problemDetails);
            }

            var githubToken = _encryptionService.Decrypt(userEntity.EncryptedGitHubAccessToken);
            var repositoriesResult = await _githubApiService.GetUserRepositoriesAsync(githubToken);

            if (repositoriesResult.IsFailure)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "The GitHub access token is expired or revoked. Please re-authenticate via GitHub."
                };

                return Unauthorized(problemDetails);
            }

            var repositories = repositoriesResult.Value.Select(r => new RepositoryResponse
            {
                Name = r.Name,
                Description = r.Description,
                StarCount = r.StarCount,
                Language = r.Language,
                Url = r.Url
            });

            return Ok(repositories);
        }
    }
}