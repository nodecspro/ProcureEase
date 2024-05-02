#region

using System.Text.RegularExpressions;

#endregion

namespace ProcureEase.Classes;

public class UserValidator
{
    private const string NamePattern = @"^[а-яА-ЯёЁa-zA-Z]+$";
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public ValidationResult Validate(User user)
    {
        var result = new ValidationResult();
        ValidateField(user.FirstName, "FirstName", NamePattern, "Имя содержит недопустимые символы.", result);
        ValidateField(user.LastName, "LastName", NamePattern, "Фамилия содержит недопустимые символы.", result);
        ValidateField(user.Patronymic, "Patronymic", NamePattern, "Отчество содержит недопустимые символы.", result,
            true);
        ValidateField(user.Email, "Email", EmailPattern, "Некорректный формат электронной почты.", result);
        return result;
    }

    private void ValidateField(string value, string fieldName, string pattern, string errorMessage,
        ValidationResult result, bool optional = false)
    {
        if ((!optional || string.IsNullOrEmpty(value)) && optional) return;
        if (!Regex.IsMatch(value, pattern))
            result.AddError(fieldName, errorMessage);
    }
}