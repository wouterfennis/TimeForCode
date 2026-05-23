using IdentityProviderMockService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route("user")]
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUser()
        {
            _logger.LogDebug("user is returned");

#pragma warning disable S1075 // URIs should not be hardcoded, this is a mock
            var user = new UserResponse
            {
                Id = 1,
                Name = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!,
                Email = "something@something.org",
                Login = "johndoe",
                NodeId = "MDQ6VXNlcjE=",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
                Company = "GitHub"
            };
#pragma warning restore S1075 // URIs should not be hardcoded

            return Ok(user);
        }

        [HttpGet("repos")]
        [ProducesResponseType(typeof(IEnumerable<RepositoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUserRepos()
        {
            _logger.LogDebug("user repos are returned");

            return Ok(GetMockRepositories());
        }

        [HttpGet("repos/{owner}/{repo}")]
        [ProducesResponseType(typeof(RepositoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRepo(string owner, string repo)
        {
            _logger.LogDebug("repo {Owner}/{Repo} is returned", owner, repo);

            var match = GetMockRepositories().FirstOrDefault(r => r.Name == repo);
            if (match != null)
            {
                return Ok(match);
            }

#pragma warning disable S1075 // URIs should not be hardcoded, this is a mock
            var fallback = new RepositoryResponse
            {
                Name = repo,
                Description = $"A mock repository for {owner}/{repo}",
                StargazersCount = 0,
                Language = "Unknown",
                HtmlUrl = $"https://github.com/{owner}/{repo}",
                IsPrivate = false
            };
#pragma warning restore S1075 // URIs should not be hardcoded

            return Ok(fallback);
        }

#pragma warning disable S1075 // URIs should not be hardcoded, this is a mock
        private static List<RepositoryResponse> GetMockRepositories()
        {
            return
            [
                new RepositoryResponse
                {
                    Name = "mock-repo-one",
                    Description = "A mock public repository",
                    StargazersCount = 42,
                    Language = "C#",
                    HtmlUrl = "https://github.com/johndoe/mock-repo-one",
                    IsPrivate = false
                },
                new RepositoryResponse
                {
                    Name = "mock-repo-two",
                    Description = "Another mock public repository",
                    StargazersCount = 7,
                    Language = "TypeScript",
                    HtmlUrl = "https://github.com/johndoe/mock-repo-two",
                    IsPrivate = false
                }
            ];
        }
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}