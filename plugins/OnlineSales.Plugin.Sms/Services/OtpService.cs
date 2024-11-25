// <copyright file="OtpService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using OnlineSales.Plugin.Sms.Interfaces;

namespace OnlineSales.Plugin.Sms.Services;

public class OtpService : IOtpService
{
    private readonly ITelegramService telegramService;
    private readonly IWhatsAppService whatsAppService;
    private readonly ISmsService smsService;

    public OtpService(ITelegramService telegramService, IWhatsAppService whatsAppService, ISmsService smsService)
    {
        this.telegramService = telegramService;
        this.whatsAppService = whatsAppService;
        this.smsService = smsService;
    }

    public async Task SendOtpAsync(string recepient, string language, string code)
    {
        var checkResult = await telegramService.CanDeliverAsync(recepient);
        if (checkResult == null)
        {
            if (!await whatsAppService.SendAuthTemplateMessage(recepient, language, code))
            {
                await smsService.SendAsync(recepient, code);
            }
        }
        else
        {
            await telegramService.SendAsync(recepient, checkResult!.request_id, code);
        }
    }

    public string GetSender()
    {
        return "OtpService";
    }
}
