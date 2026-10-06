using Hawdh.Portal.Localization;

namespace Hawdh.Portal.Tests;

public sealed class LocalizedPasswordPolicyTests
{
    private readonly LocalizedPasswordPolicyAttribute _policy = new();

    [Theory]
    [InlineData("StrongRiver4", true)]
    [InlineData("StrongRiver1234", true)]
    [InlineData("StrongRiver", false)]
    [InlineData("strongriver42", false)]
    [InlineData("STRONGRIVER42", false)]
    [InlineData("StrongRiverAA", false)]
    public void Password_policy_matches_the_identity_minimum_and_character_requirements(string password, bool expected) =>
        Assert.Equal(expected, _policy.IsValid(password));

    [Fact]
    public void Null_password_is_left_to_the_required_validator() =>
        Assert.True(_policy.IsValid(null));
}
