This is an ASP.NET Core Web API that retrieves and returns the best `n` stories from the [Hacker News API](https://github.com/HackerNews/API), sorted by score in descending order.

 Features

- Fetches and returns the top `n` stories with highest score
- Uses in-memory caching to reduce load on Hacker News API. Cache is valid for 1 min. 
- Handles large request volumes efficiently using a lock and cache pattern
- Handles partially failing stories (e.g., some nulls)
- OpenAPI (Swagger) UI available for interactive testing
- Unit tests included (using xUnit and Moq)


How to run
   a. Get sourses
   b. Open in IDE
   c. run project HackerNewsApi
   d. browser should be started with initial https://localhost:7220/swagger/index.html
   
Potential Enhancements

Given more time, here are some ideas for improving the application:

    Add retry logic with exponential backoff for httpClientFactory implementation
    For big project httpClientFactory implementation in separate file
    Implement paging (page, pageSize parameters)
    Add health checks and Prometheus metrics
    Deploy to cloud (e.g., Azure App Service or containerized via Docker)
    Use Redis for distributed caching in production
    Write logs to cloud provider
    Generally, caching for 1 minute may not be an ideal solution under constant load, as it would trigger 
       around 200 requests every minute. In reality, changes in the Hacker News API likely occur much less frequently.
       A better approach would be to subscribe to changes and update the cache only when necessary.
       Still looks like Hacker News API don't have such subscription oportunity. 
    
