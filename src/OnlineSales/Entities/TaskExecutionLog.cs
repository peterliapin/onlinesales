﻿// <copyright file="TaskExecutionLog.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineSales.Entities;

public enum TaskExecutionStatus
{
    Pending = 0,
    Completed = 1,
}

[Table("task_execution_log")]
public class TaskExecutionLog : BaseEntityWithId
{
    [Required]
    public string TaskName { get; set; } = string.Empty;

    public DateTime ScheduledExecutionTime { get; set; }

    public DateTime ActualExecutionTime { get; set; }

    public TaskExecutionStatus Status { get; set; }

    public int RetryCount { get; set; } = 0;

    public string? Comment { get; set; }
}