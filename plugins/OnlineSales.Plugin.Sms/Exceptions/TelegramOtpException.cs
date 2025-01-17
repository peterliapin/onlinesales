// <copyright file="TelegramOtpException.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Net;
using System.Runtime.Serialization;

namespace OnlineSales.Plugin.Sms.Exceptions;

[Serializable]
public class TelegramServiceCallException : Exception
{
    public TelegramServiceCallException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; private set; }
}

public class TelegramFailedResultException : Exception
{
    public TelegramFailedResultException(string? message)
        : base(message)
    {
    }
}
