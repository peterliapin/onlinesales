// <copyright file="TelegramService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OnlineSales.Plugin.Sms.DTOs;
using OnlineSales.Plugin.Sms.Exceptions;

namespace OnlineSales.Plugin.Sms.Services;

internal class TelegramService : ITelegramService, IDisposable
{
    private readonly HttpClient? client;

    public TelegramService()
    {
        var telegramConfig = SmsPlugin.Configuration.OtpGateways.Telegram;
        if (telegramConfig.AuthToken == "$SMSGATEWAYS__TELEGRAM__AUTHTOKEN")
        {
            client = null;
        }
        else
        {
            client = new HttpClient()
            {
                BaseAddress = telegramConfig.ApiUrl,
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", telegramConfig.AuthToken);
        }
    }

    public async Task<TelegramRequestStatusDto?> CanDeliverAsync(string recipient)
    {
        if(client == null)
        {
            return null;
        }

        var dto = new TelegramCheckSendAbilityDto
        {
            phone_number = recipient,
        };
        var jsonDto = JsonSerializer.Serialize(dto);
        var content = new StringContent(jsonDto, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

        var response = await client.PostAsync("checkSendAbility", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var respResult = JsonSerializer.Deserialize<TelegramResponseDto>(responseContent);
        if (respResult?.ok == false)
        {
            return null;
        }

        return respResult?.result;
    }

    public async Task SendAsync(string recipient, string requestId, string otpCode)
    {
        if (client == null)
        {
            throw new InvalidOperationException("TelegramService.SendAsync() cannot be called if the Telegram service isn't configured.");
        }

        var dto = new TelegramSendVerificationMessageDto
        {
            phone_number = recipient,
            request_id = requestId,
            code = otpCode,
        };

        var jsonDto = JsonSerializer.Serialize(dto);
        var content = new StringContent(jsonDto, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

        var response = await client.PostAsync("sendVerificationMessage", content);
        if (!response.IsSuccessStatusCode)
        {
            throw new TelegramServiceCallException("sendVerificationMessage endpoint failed", response.StatusCode);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var checkResult = JsonSerializer.Deserialize<TelegramResponseDto>(responseContent);
        if (checkResult?.ok == false)
        {
            throw new TelegramFailedResultException(checkResult.error);
        }
    }

    public void Dispose()
    {
        client?.Dispose();
    }
}
