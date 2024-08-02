﻿// <copyright file="ActivityLog.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.ComponentModel.DataAnnotations;
using OnlineSales.DataAnnotations;

namespace OnlineSales.Entities
{
    [SupportsElastic]
    public class ActivityLog : BaseEntityWithId, IHasCreatedAt
    {
        [Required]
        [Searchable]
        public int SourceId { get; set; }

        [Required]
        [Searchable]
        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        [Searchable]
        public int? ContactId { get; set; }

        [Searchable]
        public string? Ip { get; set; }

        [Required]
        [Searchable]
        public string Data { get; set; } = string.Empty;
    }
}