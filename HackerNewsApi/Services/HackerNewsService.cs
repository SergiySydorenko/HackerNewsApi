using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace HackerNewsApi.Services
{
	public class HackerNewsService : IHackerNewsService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<HackerNewsService> _logger;
		private readonly IMemoryCache _cache;
		private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

		private const string BestStoriesCacheKey = "BestStories";
		private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);

		public HackerNewsService(IHttpClientFactory httpClientFactory, ILogger<HackerNewsService> logger, IMemoryCache cache)
		{
			_httpClient = httpClientFactory.CreateClient("HackerNews");
			_logger = logger;
			_cache = cache;
		}

		public async Task<List<BestStoryDto>> GetBestStoriesAsync(int n)
		{
			var allStories = new List<BestStoryDto>();

			if (_cache.TryGetValue(BestStoriesCacheKey, out List<BestStoryDto> cachedStories))
			{
				allStories = cachedStories;
			}
			// use lock here in case a lot of people send request and nothing in cache
			// with lock only one incoming request will be processed on API others will wait for cache
			await _lock.WaitAsync();
			try
			{
				// Double-check inside lock
				if (_cache.TryGetValue(BestStoriesCacheKey, out List<BestStoryDto> cachedStories2))
				{
					allStories = cachedStories2;
				}
				else
				{
					allStories = await GetAllStoriesFromApiAsync();
				}
			}
			finally
			{
				_lock.Release();
			}

			var filtered = allStories
					.Where(x => x != null)
					.OrderByDescending(s => s!.Score)
					.Take(n).ToList();
			return filtered;
		}

		private async Task<List<BestStoryDto>?> GetAllStoriesFromApiAsync()
		{
			var storyIds = await GetBestStoryIdsAsync();
			var stories = new List<BestStoryDto>();
			var tasks = storyIds.Select(async id =>
			{
				var story = await GetStoryByIdAsync(id);
				return story;
			});

			var all = await Task.WhenAll(tasks);
			var allStories = all
				.Select(s => new BestStoryDto
				{
					Title = s!.Title,
					Uri = s.Url,
					PostedBy = s.By,
					Time = DateTimeOffset.FromUnixTimeSeconds(s.Time).UtcDateTime,
					Score = s.Score,
					CommentCount = s.Descendants
				}).ToList();

			_cache.Set(BestStoriesCacheKey, allStories, _cacheDuration);
			return allStories;
		}

		public async Task<List<int>> GetBestStoryIdsAsync()
		{
			_logger.LogInformation("Fetching best story IDs from Hacker News");

			var response = await _httpClient.GetAsync("beststories.json");

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogError("Failed to fetch best story IDs: {StatusCode}", response.StatusCode);
				return new List<int>();
			}

			var ids = await response.Content.ReadFromJsonAsync<List<int>>() ?? new List<int>();

			return ids;

		}

		public async Task<HackerNewsStory?> GetStoryByIdAsync(int id)
		{
			try
			{
				var story = await _httpClient.GetFromJsonAsync<HackerNewsStory>($"item/{id}.json");
				return story;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching story ID {Id}", id);
				return null;
			}
		}
	}
}
