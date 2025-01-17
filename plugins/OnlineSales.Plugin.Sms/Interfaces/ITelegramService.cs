// <copyright file="ITelegramService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using OnlineSales.Plugin.Sms.DTOs;

namespace OnlineSales.Interfaces;

public interface ITelegramService
{
    Task<TelegramRequestStatusDto?> CanDeliverAsync(string recipient);

    Task SendAsync(string recipient, string requestId, string otpCode);
}
