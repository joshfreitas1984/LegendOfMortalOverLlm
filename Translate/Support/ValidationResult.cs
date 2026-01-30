namespace Translate;

public class ValidationResult
{
    public bool Valid;
    public string Result = string.Empty;
    public string CorrectionPrompt = string.Empty;

    public ValidationResult() { 
    }

    public ValidationResult(bool valid, string result)
    {
        Valid = valid;
        Result = result;
    }

    public ValidationResult(string result)
    {
        Valid = !string.IsNullOrEmpty(result);
        Result = result;
    }
}