namespace Nidikwa.Cli;

internal class OperationAttribute : Attribute
{
    public OperationAttribute(string fullName, string shortName, string helpInfos, bool isDefault = false)
    {
        FullName = fullName;
        ShortName = shortName;
        HelpInfos = helpInfos;
        IsDefault = isDefault;
    }

    public string FullName { get; }
    public string ShortName { get; }
    public string HelpInfos { get; }
    public bool IsDefault { get; }
}
