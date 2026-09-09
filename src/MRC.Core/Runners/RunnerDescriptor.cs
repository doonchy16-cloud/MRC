namespace MRC.Core.Runners;

public sealed record RunnerDescriptor(
    string DirectoryPath,
    string? AgentName,
    string? GitHubUrl,
    string? RepositoryName,
    long? AgentId,
    string? WorkFolder,
    string? IdentityError)
{
    public bool HasIdentityError => !string.IsNullOrWhiteSpace(IdentityError);
}
