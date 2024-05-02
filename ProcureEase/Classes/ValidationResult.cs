namespace ProcureEase.Classes;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public Dictionary<string, string> Errors { get; } = new();

    public void AddError(string fieldName, string error)
    {
        IsValid = false;
        Errors[fieldName] = error;
    }
}