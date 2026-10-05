using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace MatchingSports.Api.ErrorHandling;

public static class VietnameseModelBindingMessages
{
    public static void LocalizeImplicitRequiredFields(MvcOptions options) =>
        options.ModelMetadataDetailsProviders.Add(new VietnameseRequiredValidationMetadataProvider());

    public static void Configure(DefaultModelBindingMessageProvider messages)
    {
        messages.SetMissingBindRequiredValueAccessor(field => $"Trường {field} là bắt buộc.");
        messages.SetMissingKeyOrValueAccessor(() => "Thiếu khóa hoặc giá trị bắt buộc.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "Nội dung yêu cầu là bắt buộc.");
        messages.SetValueMustNotBeNullAccessor(_ => "Giá trị không được để trống.");
        messages.SetAttemptedValueIsInvalidAccessor((_, field) => $"Giá trị của trường {field} không hợp lệ.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "Giá trị không hợp lệ.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"Giá trị của trường {field} không hợp lệ.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
        messages.SetValueIsInvalidAccessor(_ => "Giá trị không hợp lệ.");
        messages.SetValueMustBeANumberAccessor(field => $"Trường {field} phải là một số.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là một số.");
    }

    private sealed class VietnameseRequiredValidationMetadataProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            // MVC infers Required for non-nullable parameters after reading declared attributes.
            foreach (var required in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
            {
                if (required.ErrorMessage is null && required.ErrorMessageResourceName is null)
                    required.ErrorMessage = "Trường {0} là bắt buộc.";
            }
        }
    }
}
