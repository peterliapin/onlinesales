// <copyright file="OtpDetailsDto.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace OnlineSales.Plugin.Sms.DTOs;

public class OtpDetailsDto
{
    [Required]
    required public string Recipient { get; set; }

    [Required]
    required public string Language { get; set; }

    [Required]
    [MinLength(4)]
    [MaxLength(8)]
    [RegularExpression(@"^\d+$", ErrorMessage = "OTP code can contain digits only")]
    required public string OtpCode { get; set; }
}