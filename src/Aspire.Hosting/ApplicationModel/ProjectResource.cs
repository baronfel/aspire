// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithBindings
{
}
