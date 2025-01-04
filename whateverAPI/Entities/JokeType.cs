namespace whateverAPI.Entities;

public enum JokeType
{
    Default = 0,
    // Jokes authored by you
    Personal = 1,

    // Jokes pulled from public APIs
    Api = 2,

    // Jokes submitted by users of the app
    UserSubmission = 3,

    // Jokes fetched from third-party databases
    ThirdParty = 4,

    // Jokes curated from social media platforms
    SocialMedia = 5,

    // Miscellaneous or unknown sources
    Unknown = 6,
    Joke,
    FunnySaying,
    Discouragement,
    SelfDeprecating
}