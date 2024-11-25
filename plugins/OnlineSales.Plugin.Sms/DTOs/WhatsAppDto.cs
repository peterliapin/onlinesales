// <copyright file="WhatsAppDto.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Plugin.Sms.DTOs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class WhatsAppSendAuthTemplateMessageDto
{
    public string messaging_product { get; set; } = "whatsapp";

    public string recipient_type { get; set; } = "individual";

    public string to { get; set; } = string.Empty;

    public string type { get; set; } = "template";

    public AuthTemplateMessageDto? template { get; set; }

    public TextMessageDto? text { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class TextMessageDto
{
    public bool preview_url { get; set; }

    public string body { get; set; } = string.Empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class AuthTemplateMessageDto
{
    public string name { get; set; } = string.Empty;

    public TemplateLanguageDto language { get; set; } = new TemplateLanguageDto();

    public List<TemplateComponentDto> components { get; set; } = new List<TemplateComponentDto>
    {
        new TemplateComponentDto
        {
            type = "body",
            parameters = new List<TemplateParameterDto>
            {
                new TemplateParameterDto()
                {
                    type = "text",
                },
            },
        },
        new TemplateComponentDto
        {
            type = "button",
            sub_type = "url",
            index = 0,
            parameters = new List<TemplateParameterDto>
            {
                new TemplateParameterDto()
                {
                    type = "text",
                },
            },
        },
    };
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class TemplateLanguageDto
{
    public string code { get; set; } = string.Empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class TemplateComponentDto
{
    public string type { get; set; } = string.Empty;

    public string? sub_type { get; set; }

    public int? index { get; set; }

    public List<TemplateParameterDto>? parameters { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This is the WhatsApp naming convention")]
public class TemplateParameterDto
{
    public string type { get; set; } = string.Empty;

    public string? text { get; set; }
}
