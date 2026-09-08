namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string DatabasePath { get; set; } = StoragePathPolicy.DefaultDatabasePath;
}
