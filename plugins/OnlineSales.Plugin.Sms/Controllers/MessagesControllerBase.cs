// <copyright file="MessagesControllerBase.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OnlineSales.Entities;
using OnlineSales.Exceptions;
using OnlineSales.Plugin.Sms.Data;
using OnlineSales.Plugin.Sms.Entities;
using PhoneNumbers;
using Serilog;

namespace OnlineSales.Plugin.Sms.Controllers;

public abstract class MessagesControllerBase : Controller
{
    private readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
    private readonly SmsDbContext dbContext;

    protected MessagesControllerBase(SmsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    protected async Task<ActionResult> SendMessage(string accessToken, string phone, string message)
    {
        try
        {
            if (accessToken == null || accessToken.Replace("Bearer ", string.Empty) != SmsPlugin.Configuration.SmsAccessKey)
            {
                return new UnauthorizedResult();
            }

            var recipient = GetRecipient(phone);
            var sender = GetSender(recipient);
            var smsLog = await AddLog(recipient, sender, message);

            await SendMessage(recipient, message);
            smsLog.Status = SmsLog.SmsStatus.Sent;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send message to {0}: {1}", phone, message);
            throw;
        }
        finally
        {
            await dbContext.SaveChangesAsync();
        }

        return Ok();
    }

    protected abstract string GetSender(string recipient);

    protected abstract Task SendMessage(string recipient, string message);

    private string GetRecipient(string phone)
    {
        var recipient = string.Empty;
        try
        {
            var phoneNumber = phoneNumberUtil.Parse(phone, string.Empty);

            recipient = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
        }
        catch (NumberParseException npex)
        {
            ModelState.AddModelError(npex.ErrorType.ToString(), npex.Message);
        }

        if (!ModelState.IsValid)
        {
            throw new InvalidModelStateException(ModelState);
        }

        return recipient;
    }

    private async Task<SmsLog> AddLog(string recipient, string sender, string message)
    {
        var smsLog = new SmsLog
        {
            Sender = sender,
            Recipient = recipient,
            Message = message,
            Status = SmsLog.SmsStatus.NotSent,
        };

        dbContext.SmsLogs!.Add(smsLog);
        await dbContext.SaveChangesAsync();

        return smsLog;
    }
}
