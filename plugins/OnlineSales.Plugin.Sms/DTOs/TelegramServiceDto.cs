// <copyright file="TelegramServiceDto.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Plugin.Sms.DTOs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the Telegram name convinion")]
public class TelegramCheckSendAbilityDto
{
    required public string phone_number { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the Telegram name convinion")]
public class TelegramSendVerificationMessageDto
{
    required public string phone_number { get; set; }

    required public string request_id { get; set; }

    required public string code { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the Telegram name convinion")]
public class TelegramResponseDto
{
    public bool ok { get; set; }

    public string? error { get; set; }

    public TelegramRequestStatusDto? result { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the Telegram name convinion")]
public class TelegramRequestStatusDto
{
    public string request_id { get; set; } = string.Empty; // Unique identifier of the verification request.

    public string phone_number { get; set; } = string.Empty; // The phone number to which the verification code was sent, in the E.164 format.

    public float request_cost { get; set; } // Total request cost incurred by either checkSendAbility or sendVerificationMessage.

    public float? remaining_balance { get; set; } // Optional. Remaining balance in credits. Returned only in response to a request that incurs a charge.
}
