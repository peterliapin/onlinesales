﻿// <copyright file="FileExtensionValidateAttribute.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using OnlineSales.Configuration;

namespace OnlineSales.DataAnnotations
{
    public class FileExtensionValidateAttribute : ValidationAttribute
    {
        private readonly string type;

        public FileExtensionValidateAttribute(string type)
        {
            this.type = type;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            string[] listOfExt;

            if (type.Equals("Image"))
            {
                var configuration = (IOptions<ImagesConfig>)validationContext!.GetService(typeof(IOptions<ImagesConfig>)) !;
                listOfExt = configuration.Value.Extensions;
            }
            else if (type.Equals("EmailAttachment"))
            {
                var configuration = (IOptions<EmailAttachmentConfig>)validationContext!.GetService(typeof(IOptions<EmailAttachmentConfig>)) !;
                listOfExt = configuration.Value.Extensions;
            }
            else
            {
                return new ValidationResult("Invalid request type (check annotation)");
            }

            var file = value as IFormFile;

            if (file == null)
            {
                return ValidationResult.Success!;
            }

            string currentExt = Path.GetExtension(file.FileName.ToLower());

            var hasMatchingExt = from ext in listOfExt! where ext == currentExt select ext;

            if (!hasMatchingExt.Any())
            {
                return new ValidationResult("Invalid file extension");
            }

            return ValidationResult.Success!;
        }
    }
}
