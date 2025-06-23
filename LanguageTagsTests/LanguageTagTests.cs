using Xunit;

namespace ptr727.LanguageTags.Tests;

public class LanguageTagTests(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly Fixture _fixture = fixture;
}
