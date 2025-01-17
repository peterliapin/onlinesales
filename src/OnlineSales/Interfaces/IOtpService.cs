// <copyright file="IOtpService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Interfaces;

public interface IOtpService
{
    Task SendOtpAsync(string recepient, string language, string code);

    string GetSender();
}
