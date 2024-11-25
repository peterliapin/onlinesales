// <copyright file="PluginSettings.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Plugin.Sms.Configuration;

public class PluginConfig
{
    public string SmsAccessKey { get; set; } = string.Empty;

    public GatewaysConfig SmsGateways { get; set; } = new GatewaysConfig();

    public OtpGatewaysConfig OtpGateways { get; set; } = new OtpGatewaysConfig();

    public List<CountryGatewayConfig> SmsCountryGateways { get; set; } = new List<CountryGatewayConfig>();
}

public class GatewaysConfig
{
    public AmazonSnsConfig AmazonSns { get; set; } = new AmazonSnsConfig();

    public SmscConfig Smsc { get; set; } = new SmscConfig();

    public SmscConfig SmscKz { get; set; } = new SmscConfig();

    public GetshoutoutConfig Getshoutout { get; set; } = new GetshoutoutConfig();

    public NotifyLkConfig NotifyLk { get; set; } = new NotifyLkConfig();

    public TwilioConfig Twilio { get; set; } = new TwilioConfig();

    public WhatsAppConfig WhatsApp { get; set; } = new WhatsAppConfig();
}

public class CountryGatewayConfig
{
    public string Code { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;
}

/// <summary>
/// API Documentation: https://docs.aws.amazon.com/sns/latest/api/welcome.html.
/// </summary>
public class AmazonSnsConfig
{
    public string SenderId { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string DefaultRegion { get; set; } = string.Empty;
}

/// <summary>
/// API Documentation: https://developers.getshoutout.com/#liteapimessage_send.
/// </summary>
public class GetshoutoutConfig
{
    public string SenderId { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// API Documentation: https://developer.notify.lk/api-endpoints/.
/// </summary>
public class NotifyLkConfig
{
    public string SenderId { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// API Documentation: https://smsc.ru/api/.
/// </summary>
public class SmscConfig
{
    public string SenderId { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// API Documentation: https://www.twilio.com/docs/sms/quickstart/csharp-dotnet-core.
/// </summary>
public class TwilioConfig
{
    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;
}

public class WhatsAppConfig
{
    public Uri ApiUrl { get; set; } = new Uri("https://graph.facebook.com/");

    public string ApiVersion { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public string BusinessPhoneId { get; set; } = string.Empty;

    public bool EnableLinkPreviewByDefault { get; set; } = true;
}

public class OtpGatewaysConfig
{
    public TelegramConfig Telegram { get; set; } = new TelegramConfig();

    public WhatsAppOtpConfig WhatsApp { get; set; } = new WhatsAppOtpConfig();
}

public class TelegramConfig
{
    public Uri ApiUrl { get; set; } = new Uri("https://gatewayapi.telegram.org/");

    public string AuthToken { get; set; } = string.Empty;
}

public class WhatsAppOtpConfig
{
    public WhatsAppTemplateMessageConfig Default { get; set; } = new WhatsAppTemplateMessageConfig();

    public Dictionary<string, WhatsAppTemplateMessageConfig> Language { get; set; } = new Dictionary<string, WhatsAppTemplateMessageConfig>();

    public WhatsAppTemplateMessageConfig GetByLanguage(string? language)
    {
        return language != null && Language.TryGetValue(language, out var config)
            ? config
            : Default;
    }
}

public class WhatsAppTemplateMessageConfig
{
    public string TemplateName { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;
}
