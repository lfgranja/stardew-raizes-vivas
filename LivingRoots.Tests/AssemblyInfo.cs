using Xunit;

// The test suite installs process-global game state (Game1.content, Game1.sounds,
// Game1.currentLocation, LocationContexts) through GameStateFixture. Several
// classes install or mutate that state in their arrange phase, so running
// classes in parallel makes the outcome depend on scheduling — a class that
// seeds state another class is mid-way through mutating fails with a
// NullReferenceException from inside the game code rather than from the code
// under test. Tests within a class still run sequentially in method order.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
