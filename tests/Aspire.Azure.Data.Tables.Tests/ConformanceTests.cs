// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Data.Tables.Tests;

public class ConformanceTests : ConformanceTests<TableServiceClient, AzureDataTablesSettings>
{
    // Authentication method: Azure AD User Account
    // Roles: Storage Table Data Reader, Storage Table Data Contributor
    private const string ServiceUri = "https://aspirestoragetests.table.core.windows.net/";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Data.Tables.TableServiceClient";

    protected override string JsonSchemaPath => "src/Components/Aspire.Azure.Data.Tables/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Data": {
                "Tables": {
                  "ServiceUri": "http://YOUR_URI",
                  "HealthChecks": false,
                  "Tracing": true,
                  "ClientOptions": {
                    "EnableTenantDiscovery": true,
                    "Retry": {
                      "Mode": "Exponential",
                      "Delay": "PT3S"
                    }
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "Data":{ "Tables": { "ServiceUri": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Data":{ "Tables": { "ServiceUri": "http://YOUR_URI", "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Data":{ "Tables": { "ServiceUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Data":{ "Tables": { "ServiceUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"NetworkTimeout": "3S"}}}}}}}""", "Value does not match format \"duration\"")
        };

    protected override string[] RequiredLogCategories => new string[]
    {
        "Azure.Identity",
        "Azure.Core"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:Data:Tables", key, "ServiceUri"), ServiceUri),
            new(CreateConfigKey("Aspire:Azure:Data:Tables", key, "ClientOptions:Retry:MaxRetries"), "0")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureDataTablesSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureTableService(configureSettings: ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureTableService(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureDataTablesSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(AzureDataTablesSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetMetrics(AzureDataTablesSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureDataTablesSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void TriggerActivity(TableServiceClient service)
    {
        string tableName = Guid.NewGuid().ToString();
        service.CreateTableIfNotExists(tableName);
        service.DeleteTable(tableName);
    }

    private static bool GetCanConnect()
    {
        TableClientOptions clientOptions = new();
        clientOptions.Retry.MaxRetries = 0; // don't enable retries (test runs few times faster)
        TableServiceClient tableClient = new(new Uri(ServiceUri), new DefaultAzureCredential(), clientOptions);

        try
        {
            // "test" is a pre-existing table
            tableClient.GetTableClient("test").Query<TableEntity>(filter: "false").FirstOrDefault();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
