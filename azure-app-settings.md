# Azure Web App Configuration

## Required Application Settings

Add these settings in the Azure Portal under **Configuration > Application settings**:

### Node.js Configuration
- **WEBSITE_NODE_DEFAULT_VERSION**: `18.17.0`
- **WEBSITE_NPM_DEFAULT_VERSION**: `9.6.7`

### Application Insights Configuration
- **APPINSIGHTS_INSTRUMENTATIONKEY**: `your-instrumentation-key`
- **ApplicationInsightsAgent_EXTENSION_VERSION**: `~3`
- **APPLICATIONINSIGHTS_CONNECTION_STRING**: `your-connection-string`

### Runtime Configuration
- **WEBSITE_RUN_FROM_PACKAGE**: `1`
- **SCM_DO_BUILD_DURING_DEPLOYMENT**: `false`

## Steps to Configure in Azure Portal

1. Go to your Azure Web App in the portal
2. Navigate to **Configuration** in the left menu
3. Click **Application settings**
4. Add each setting with **+ New application setting**
5. Click **Save** after adding all settings
6. Restart your web app

## Alternative: Using Azure CLI

```bash
az webapp config appsettings set --resource-group your-resource-group --name whateverbruh --settings WEBSITE_NODE_DEFAULT_VERSION=18.17.0 WEBSITE_NPM_DEFAULT_VERSION=9.6.7 SCM_DO_BUILD_DURING_DEPLOYMENT=false
```

## web.config for Additional Control

Place this in your wwwroot folder to override Node.js settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\whateverAPI.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="WEBSITE_NODE_DEFAULT_VERSION" value="18.17.0" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
``` 