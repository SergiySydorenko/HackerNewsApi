using HackerNewsApi.Models;

namespace HackerNewsApi.Services
{
	public interface IHackerNewsService
	{
		Task<List<BestStoryDto>> GetBestStoriesAsync(int count);
	}
}

