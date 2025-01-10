// // Endpoints
// var apiGroup = app.MapGroup("/api");
// var jokeGroup = apiGroup.MapGroup("/jokes").WithTags("Jokes");
// var userGroup = apiGroup.MapGroup("/user").WithTags("User");
//
// // Get All Jokes
// jokeGroup.MapGet("/", async Task<IResult> (
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var jokes = await jokeService.GetJokes(ct);
//     return jokes is not null && jokes.Count != 0
//         ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//         : context.CreateNotFoundProblem("Jokes", "all");
// })
// .WithName("GetJokes")
// .WithDescription("Retrieves all jokes from the database with pagination")
// .WithSummary("Get all jokes")
// .WithOpenApi()
// .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesProblem(StatusCodes.Status401Unauthorized)
// .RequireAuthorization();
//
// // Get Joke by ID
// jokeGroup.MapGet("/{id:guid}", async Task<IResult> (
//     [FromRoute] Guid id, 
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var joke = await jokeService.GetJokeById(id, ct);
//     return joke is not null 
//         ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
//         : context.CreateNotFoundProblem("Joke", id.ToString());
// })
// .WithName("GetJokeById")
// .WithDescription("Retrieves a specific joke by its unique identifier")
// .WithSummary("Get a joke by ID")
// .WithOpenApi()
// .Produces<JokeResponse>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Create New Joke
// jokeGroup.MapPost("/", async Task<IResult> (
//     CreateJokeRequest request, 
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var joke = Mapper.CreateRequestToJoke(request);
//     var created = await jokeService.CreateJoke(joke, ct);
//     var response = Mapper.JokeToJokeResponse(created);
//     return response is not null 
//         ? TypedResults.Created($"/api/jokes/{created.Id}", response)
//         : context.CreateUnprocessableEntityProblem("Create Joke");
// })
// .WithName("CreateJoke")
// .WithDescription("Creates a new joke with the provided content and metadata")
// .WithSummary("Create a new joke")
// .WithOpenApi()
// .Accepts<CreateJokeRequest>("application/json")
// .Produces<JokeResponse>(StatusCodes.Status201Created)
// .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest)
// .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>();
//
// // Get Jokes by Type
// jokeGroup.MapGet("/type", async Task<IResult> (
//     [AsParameters] FilterRequest request, 
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var jokes = await jokeService.GetJokesByType(request, ct);
//     return jokes.Count != 0
//         ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//         : context.CreateNotFoundProblem("Jokes", $"type {request.Type}");
// })
// .WithName("GetJokesByType")
// .WithDescription("Retrieves jokes filtered by type with optional sorting and pagination")
// .WithSummary("Get jokes by type")
// .WithOpenApi()
// .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest)
// .AddEndpointFilter<ValidationFilter<FilterRequest>>();
//
// // Update Joke
// jokeGroup.MapPut("/{id:guid}", async Task<IResult> (
//     [FromRoute] Guid id, 
//     UpdateJokeRequest request, 
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var joke = Mapper.UpdateRequestToJoke(id, request);
//     var updated = await jokeService.UpdateJoke(joke, ct);
//     return updated is not null
//         ? TypedResults.Ok(Mapper.JokeToJokeResponse(updated))
//         : context.CreateNotFoundProblem("Joke", id.ToString());
// })
// .WithName("UpdateJoke")
// .WithDescription("Updates an existing joke's content and metadata")
// .WithSummary("Update a joke")
// .WithOpenApi()
// .Accepts<UpdateJokeRequest>("application/json")
// .Produces<JokeResponse>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest)
// .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
// .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();
//
// // Delete Joke
// jokeGroup.MapDelete("/{id:guid}", async Task<IResult> (
//     [FromRoute] Guid id, 
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var result = await jokeService.DeleteJoke(id, ct);
//     return result
//         ? TypedResults.NoContent()
//         : context.CreateNotFoundProblem("Joke", id.ToString());
// })
// .WithName("DeleteJoke")
// .WithDescription("Permanently removes a joke from the database")
// .WithSummary("Delete a joke")
// .WithOpenApi()
// .Produces(StatusCodes.Status204NoContent)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Get Random Joke
// jokeGroup.MapGet("/random", async Task<IResult> (
//     IJokeService jokeService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     var joke = await jokeService.GetRandomJoke(ct);
//     return joke is not null
//         ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
//         : context.CreateNotFoundProblem("Jokes", "random");
// })
// .WithName("GetRandomJoke")
// .WithDescription("Retrieves a random joke from the available collection")
// .WithSummary("Get a random joke")
// .WithOpenApi()
// .Produces<JokeResponse>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound);
//
// // Search Jokes
// jokeGroup.MapGet("/search", async Task<IResult> (
//     [FromQuery(Name = "q")] string query,
//     IJokeService jokeService,
//     HttpContext context,
//     CancellationToken ct) =>
// {
//     if (string.IsNullOrWhiteSpace(query))
//     {
//         return context.CreateBadRequestProblem("Search query cannot be empty");
//     }
//
//     var jokes = await jokeService.SearchJokes(query, ct);
//     return jokes?.Count > 0
//         ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
//         : context.CreateNotFoundProblem("Jokes", $"matching query '{query}'");
// })
// .WithName("SearchJokes")
// .WithDescription("Searches for jokes containing the specified query in their content or tags")
// .WithSummary("Search for jokes")
// .WithOpenApi()
// .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest);
//
// // Get External Joke
// jokeGroup.MapGet("/whatever", async Task<IResult> (
//     JokeApiService jokeApiService, 
//     HttpContext context, 
//     CancellationToken ct) =>
// {
//     try
//     {
//         var joke = await jokeApiService.GetExternalJoke(ct);
//         return joke is not null
//             ? TypedResults.Ok(joke)
//             : context.CreateNotFoundProblem("External Joke", "random");
//     }
//     catch (HttpRequestException ex)
//     {
//         return context.CreateExternalServiceProblem(
//             "Joke API", 
//             "Failed to fetch joke from external service", 
//             ex);
//     }
// })
// .WithName("GetWhateverJoke")
// .WithDescription("Retrieves a random joke from a third-party API")
// .WithSummary("Get a joke from a third-party API")
// .WithOpenApi()
// .Produces<Joke>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status404NotFound)
// .ProducesProblem(StatusCodes.Status502BadGateway);
//
// // User Login
// userGroup.MapPost("/login", async Task<IResult> (
//     [FromBody] UserLoginRequest request, 
//     JwtTokenService jwtTokenService, 
//     HttpContext context) =>
// {
//     var jwtToken = jwtTokenService.GenerateToken(request.Username, request.Email);
//     return !string.IsNullOrEmpty(jwtToken)
//         ? TypedResults.Ok(new { request.Username, Token = jwtToken })
//         : context.CreateUnauthorizedProblem("Invalid credentials provided");
// })
// .WithName("UserLogin")
// .WithDescription("Authenticates a user and returns a JWT token for subsequent requests")
// .WithSummary("Login user")
// .WithOpenApi()
// .Accepts<UserLoginRequest>("application/json")
// .Produces<object>(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status401Unauthorized)
// .ProducesValidationProblem(StatusCodes.Status400BadRequest)
// .AddEndpointFilter<ValidationFilter<UserLoginRequest>>();
//
// // User Logout
// userGroup.MapPost("/logout", async Task<IResult> (
//     [FromServices] JwtTokenService jwtTokenService, 
//     HttpContext context) =>
// {
//     var token = jwtTokenService.GetToken();
//     if (string.IsNullOrEmpty(token))
//     {
//         return context.CreateUnauthorizedProblem("No valid authentication token found");
//     }
//
//     jwtTokenService.InvalidateToken(token);
//     return TypedResults.Ok();
// })
// .WithName("UserLogout")
// .WithDescription("Invalidates the current user's JWT token")
// .WithSummary("Logout user")
// .WithOpenApi()
// .Produces(StatusCodes.Status200OK)
// .ProducesProblem(StatusCodes.Status401Unauthorized);
//
// app.Run();