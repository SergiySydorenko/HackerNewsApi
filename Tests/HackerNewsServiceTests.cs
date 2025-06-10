using System.Net;
using System.Net.Http.Json;
using HackerNewsApi.Models;
using HackerNewsApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace HackerNewsApi.Tests
{
	public class HackerNewsServiceTests
	{
		private readonly Mock<ILogger<HackerNewsService>> _loggerMock = new();
		private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

		private HackerNewsService CreateService(List<int> storyIds, List<HackerNewsStory> storyDetails)
		{
			var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

			// Mock beststories.json
			handlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath.Contains("beststories.json")),
					ItExpr.IsAny<CancellationToken>()
				)
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = JsonContent.Create(storyIds)
				});

			// Mock individual story items
			foreach (var story in storyDetails)
			{
				handlerMock.Protected()
					.Setup<Task<HttpResponseMessage>>(
						"SendAsync",
						ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath.Contains($"item/{story.Id}.json")),
						ItExpr.IsAny<CancellationToken>()
					)
					.ReturnsAsync(new HttpResponseMessage
					{
						StatusCode = HttpStatusCode.OK,
						Content = JsonContent.Create(story)
					});
			}

			var client = new HttpClient(handlerMock.Object)
			{
				BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
			};

			var factoryMock = new Mock<IHttpClientFactory>();
			factoryMock.Setup(x => x.CreateClient("HackerNews")).Returns(client);

			return new HackerNewsService(factoryMock.Object, _loggerMock.Object, _cache);
		}

		[Fact]
		public async Task GetBestStoriesAsync_Returns_TopSortedStories()
		{
			// Arrange
			var storyIds = new List<int> { 1, 2, 3 };
			var stories = new List<HackerNewsStory>
		{
			new() { Id = 1, Title = "Story 1", Score = 50, By = "user1", Url = "http://1.com", Time = 1717572000, Descendants = 5 },
			new() { Id = 2, Title = "Story 2", Score = 70, By = "user2", Url = "http://2.com", Time = 1717572100, Descendants = 10 },
			new() { Id = 3, Title = "Story 3", Score = 60, By = "user3", Url = "http://3.com", Time = 1717572200, Descendants = 7 }
		};

			var service = CreateService(storyIds, stories);

			// Act
			var result = await service.GetBestStoriesAsync(2);

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Equal("Story 2", result[0].Title); // Score: 70
			Assert.Equal("Story 3", result[1].Title); // Score: 60
		}

		[Fact]
		public async Task GetBestStoriesAsync_UsesCacheOnSubsequentCalls()
		{
			// Arrange
			var storyIds = new List<int> { 1 };
			var stories = new List<HackerNewsStory>
			{
				new() { Id = 1, Title = "Story 1", Score = 100, By = "user1", Url = "http://1.com", Time = 1717572000, Descendants = 2 }
			};

			var service = CreateService(storyIds, stories);

			// Act: First call
			var first = await service.GetBestStoriesAsync(1);
			// Act: Second call (should hit cache)
			var second = await service.GetBestStoriesAsync(1);

			// Assert
			Assert.Single(first);
			Assert.Single(second);
			Assert.Equal(first[0].Title, second[0].Title);
		}

		[Fact]
		public async Task GetBestStoriesAsync_HandlesNullStoryGracefully()
		{
			// Arrange
			var storyIds = new List<int> { 1, 2 };
			var validStory = new HackerNewsStory
			{
				Id = 1,
				Title = "Valid",
				Score = 10,
				By = "a",
				Url = "http://a.com",
				Time = 1717572000,
				Descendants = 1
			};

			// Only one story returned, other will cause exception (simulate null)
			var service = CreateService(storyIds, new List<HackerNewsStory> { validStory });

			// Act
			var result = await service.GetBestStoriesAsync(1);

			// Assert
			Assert.Single(result);
			Assert.Equal("Valid", result[0].Title);
		}
	}
}