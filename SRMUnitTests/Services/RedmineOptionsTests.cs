using SRMCore.Configuration;

namespace SRMUnitTests.Services;

public class RedmineOptionsTests
{
    [Test]
    public void BuildPublicIssueUrl_UsesPublicBaseUrl_WhenConfigured()
    {
        var options = new RedmineOptions
        {
            BaseUrl = "http://srm-redmine:3000/",
            PublicBaseUrl = "http://localhost:3000/"
        };

        var result = options.BuildPublicIssueUrl("2");

        Assert.That(result, Is.EqualTo("http://localhost:3000/issues/2"));
    }

    [Test]
    public void BuildPublicIssueUrl_FallsBackToServiceBaseUrl()
    {
        var options = new RedmineOptions { BaseUrl = "http://redmine/" };

        var result = options.BuildPublicIssueUrl("42");

        Assert.That(result, Is.EqualTo("http://redmine/issues/42"));
    }
}
