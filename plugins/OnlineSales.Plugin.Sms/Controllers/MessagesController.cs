﻿// <copyright file="MessagesController.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSales.Data;
using OnlineSales.Plugin.Sms.DTOs;
using PhoneNumbers;
using Serilog;

namespace OnlineSales.Plugin.Sms.Controllers;

[Route("api/messages")]
public class MessagesController : Controller
{
    protected readonly DbContext dbContext;
    protected readonly ISmsService smsService;
    protected readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();

    public MessagesController(ApiDbContext dbContext, ISmsService smsService)
    {
        this.dbContext = dbContext;
        this.smsService = smsService;
    }

    [HttpPost]
    [Route("sms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendSms(
        [FromBody]SmsDetailsDto smsDetails,
        [FromHeader(Name = "Authentication")]string accessToken)
    {
        try
        {
            if (accessToken == null || accessToken.Replace("Bearer ", string.Empty) != SmsPlugin.Settings.SmsAccessKey)
            {
                return new UnauthorizedResult();
            }

            string recipient = string.Empty;

            try
            {
                var phoneNumber = phoneNumberUtil.Parse(smsDetails.Recipient, string.Empty);

                recipient = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
            }
            catch (NumberParseException npex)
            {
                ModelState.AddModelError(npex.ErrorType.ToString(), npex.Message);
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            await smsService.SendAsync(recipient, smsDetails.Message);

            return Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send SMS message to {0}: {1}", smsDetails.Recipient, smsDetails.Message);

            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: ex.Message,
                detail: ex.StackTrace);
        }
    }
}

