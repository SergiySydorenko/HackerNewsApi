using HackerNewsApi.Models;
using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StoriesController : ControllerBase
	{
		private readonly IHackerNewsService _hackerNewsService;
		private readonly ILogger<StoriesController> _logger;

		public StoriesController(IHackerNewsService hackerNewsService, ILogger<StoriesController> logger)
		{
			_hackerNewsService = hackerNewsService;
			_logger = logger;
		}

		/// <summary>
		/// Retrieves the top N best stories from Hacker News, sorted by score in descending order.
		/// </summary>
		/// <param name="n">The number of top stories to return (1–200).</param>
		/// <returns>List of stories including title, URL, author, post time, score, and comment count.</returns>
		/// <response code="200">Returns the sorted list of best stories</response>
		/// <response code="400">If n is not within the allowed range</response>
		/// <response code="500">If an internal server error occurs</response>
		[HttpGet("best/{n:int}")]
		[ProducesResponseType(typeof(List<BestStoryDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<List<BestStoryDto>>> GetBestStories(int n)
		{
			if (n <= 1 || n > 200)
			{
				return BadRequest("Please provide a number between 1 and 200.");
			}		

			try
			{
				var stories = await _hackerNewsService.GetBestStoriesAsync(n);
				return Ok(stories);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while retrieving best stories.");
				return StatusCode(500, "Internal server error");
			}
		}
	}
}
