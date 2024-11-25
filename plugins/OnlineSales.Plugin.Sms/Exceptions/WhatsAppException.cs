// <copyright file="WhatsAppException.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace OnlineSales.Plugin.Sms.Exceptions;

public class WhatsAppException : Exception
{
    public WhatsAppException(string? message)
        : base(message)
    {
    }
}
