using System.ComponentModel.DataAnnotations;

namespace Brizbee.Dashboard.Server.Validations
{
    public class FullNameLengthValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string valueString = (string)value;

            // Entire string is limited to 159 characters by QuickBooks
            if (valueString.Length > 159)
                return new ValidationResult("Full name in QuickBooks can only be 159 characters.",
                    new[] { validationContext.MemberName });

            var split = valueString.Split(':');

            // Each name string is limited to 41 characters by QuickBooks
            foreach (var element in split)
            {
                if (element.Length > 41)
                    return new ValidationResult("Names in QuickBooks can only be 41 characters.",
                        new[] { validationContext.MemberName });
            }

            return ValidationResult.Success;
        }
    }
}
