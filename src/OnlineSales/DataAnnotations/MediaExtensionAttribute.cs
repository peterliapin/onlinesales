﻿// <copyright file="MediaExtensionAttribute.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using OnlineSales.Configuration;
using OnlineSales.Helpers;

namespace OnlineSales.DataAnnotations
{
    public class MediaExtensionAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var configuration = (IOptions<MediaConfig>?)validationContext!.GetService(typeof(IOptions<MediaConfig>));
            if (configuration == null)
            {
                throw new MissingConfigurationException("Failed to resolve IOptions<ImagesConfig> object.");
            }

            var file = value as IFormFile;

            if (file == null)
            {
                return ValidationResult.Success!;
            }

            var fileExtension = Path.GetExtension(file.FileName);
            if (!configuration.Value.Extensions.Contains(fileExtension))
            {
                return new ValidationResult("Invalid file extension.");
            }

            var fileLength = file.Length;

            var fileLengthSizeInfo = configuration.Value.MaxSize.FirstOrDefault(info => info.Extension == fileExtension);
            if (fileLengthSizeInfo == null)
            {
                fileLengthSizeInfo = configuration.Value.MaxSize.FirstOrDefault(info => info.Extension == "default");
                if (fileLengthSizeInfo == null)
                {
                    throw new MissingConfigurationException("Failed to resolve default value for upload media.");
                }
            }

            var fileLengthAllowedSize = StringHelper.GetSizeInBytesFromString(fileLengthSizeInfo.MaxSize);

            if (fileLength > fileLengthAllowedSize)
            {
                return new ValidationResult($"Invalid file length. Expected {fileLengthAllowedSize} for '{fileExtension}'. Got {fileLength}.");
            }

            return ValidationResult.Success!;
        }
    }
}