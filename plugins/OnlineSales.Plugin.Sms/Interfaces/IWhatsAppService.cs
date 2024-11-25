// <copyright file="IWhatsAppService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Plugin.Sms.Interfaces;

public interface IWhatsAppService
{
    /// <summary>
    /// Send an OTP code immediately, using the AuthTemlate.
    /// We can add as many SendXXXTemplateMessage methods as templates exist (when it's required).
    /// All these methods will have different set of parameters related to template.
    /// </summary>
    /// <param name="phone">Recipient's phone number.</param>
    /// <param name="language">Language, like "en" or "dk" etc.</param>
    /// <param name="otpCode">The OTP Code of any symbold up to 15 ones.</param>
    /// <returns>true - the code has been sent, false - an error is occured.</returns>
    Task<bool> SendAuthTemplateMessage(string phone, string language, string otpCode);

    /// <summary>
    /// Send a text message to user, if a 24-hour timer called a customer service window has started by his message.
    /// </summary>
    /// <param name="phone">Recipient's phone number.</param>
    /// <param name="text">Message text.</param>
    /// <param name="enableLinkPreview">If the message contains a link (that begins with http:// or https://), WhapsApp can show a preview.</param>
    /// <returns>true - the message has been sent, false - an error is occured.</returns>
    Task<bool> SendTextMessage(string phone, string text, bool enableLinkPreview);
}
