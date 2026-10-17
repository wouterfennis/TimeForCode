using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    public class UserController : ControllerBase
    {
        private readonly IAccountInformationRepository _accountInformationRepository;
        private readonly IGithubApiService _githubApiService;
        private readonly ISender _sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        public UserController(
            IAccountInformationRepository accountInformationRepository,
            IGithubApiService githubApiService,
            ISender sender)
        {
            _accountInformationRepository = accountInformationRepository;
            _githubApiService = githubApiService;
            _sender = sender;
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
        /// Delete the account of the user: removes the profile and refresh tokens and anonymises the contributions.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("", Name = "DeleteAccount")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAccountAsync()
        {
            var userId = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!;

            var result = await _sender.Send(new DeleteAccountCommand { UserId = userId });

            if (result.IsFailure)
            {
                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "User not found",
                        Detail = $"User with id {userId} not found"
                    });
                }

                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Account could not be deleted",
                    Detail = "The account was kept because a dependent service could not be updated. Try again later."
                });
            }

            TokenCookies.Expire(HttpContext.Response);

            return NoContent();
        }

        /// <summary>
        /// Get user's public repositories from GitHub.
        /// </summary>
        /// <returns></returns>
        [HttpGet("repositories")]
        [ProducesResponseType(typeof(IEnumerable<RepositoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
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

            var repositoriesResult = await _githubApiService.GetUserRepositoriesAsync(userEntity.Login);

            if (repositoriesResult.IsFailure)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Bad Gateway",
                    Detail = "An upstream error occurred while contacting GitHub."
                };

                return StatusCode(StatusCodes.Status502BadGateway, problemDetails);
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

        /// <summary>
        /// Get a single public repository from GitHub by owner and repository name.
        /// </summary>
        /// <param name="owner">The GitHub username or organisation that owns the repository (at most 39 characters; letters, digits and single hyphens).</param>
        /// <param name="repo">The repository name (at most 100 characters; letters, digits, dots, underscores and hyphens).</param>
        /// <returns></returns>
        [HttpGet("repositories/{owner}/{repo}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RepositoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRepositoryAsync(
            [StringLength(ApiConstraints.MaxGithubOwnerLength, MinimumLength = 1), RegularExpression(ApiConstraints.GithubOwnerPattern)] string owner,
            [StringLength(ApiConstraints.MaxGithubRepositoryNameLength, MinimumLength = 1), RegularExpression(ApiConstraints.GithubRepositoryNamePattern)] string repo)
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

            var repositoryResult = await _githubApiService.GetRepositoryAsync(owner, repo);

            if (repositoryResult.IsFailure)
            {
                if (repositoryResult.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Repository not found",
                        Detail = $"Repository {owner}/{repo} was not found on GitHub."
                    };

                    return NotFound(problemDetails);
                }

                var upstreamProblemDetails = new ProblemDetails
                {
                    Title = "Bad Gateway",
                    Detail = $"An upstream error occurred while contacting GitHub: {repositoryResult.ErrorMessage}"
                };

                return StatusCode(StatusCodes.Status502BadGateway, upstreamProblemDetails);
            }

            var repository = new RepositoryResponse
            {
                Name = repositoryResult.Value.Name,
                Description = repositoryResult.Value.Description,
                StarCount = repositoryResult.Value.StarCount,
                Language = repositoryResult.Value.Language,
                Url = repositoryResult.Value.Url
            };

            return Ok(repository);
        }
    }
}