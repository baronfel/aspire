// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureRedisComponent : IAzureComponent
{
    public ComponentMetadataCollection Annotations { get; } = new();

    public string? HostName { get; set; }
    public string? AccessKey { get; set; }
    public int? Port { get; internal set; }
    public int? SslPort { get; internal set; }
}
