
## Main Project Build Status:

[![Build Status](https://dev.azure.com/pworthey007/Periodically%20Peckish%20Artisans/_apis/build/status/Periodically-Peckish-Artisans.project-browser-backend?branchName=master)](https://dev.azure.com/pworthey007/Periodically%20Peckish%20Artisans/_build/latest?definitionId=2&branchName=master)

## Instructions to set up CI/CD on your own if you fork:

My advice would be: you should probably know what you're doing if you're doing this. Technologies involved: Azure DevOps build pipelines, Azure, ARM Templates, and Google Firebase authentication. The currently configured setup is to deploy when changes are integrated into master.

1. Sign up for a project with Google Firebase authentication enabled. Download json credentials for a configured admin service account.
2. Gather your Azure subscription and Azure DevOps project
3. Add service connection in Azure DevOps to connect to your Azure subscription to azure dev ops (write down name of service connection)
4. Connect a build definition on Azure DevOps to GitHub with azurepipelines-deploy.yml selected
5. Edit the build definition's variables with the following:
* $(AppName) = Name of the function app (should be globally unique since it forms basis of URL)
* $(AzureLocation) = Where the server farm will be located ie. 'West US'
* $(AzureResourceGroupName) = Any name for the resource group to be created/updated
* $(AzureServiceConnectionName) = The name of the service connection name you made in Azure DevOps
* $(AzureStorageAccountName) = The name of the storage account (no non-alphanumeric characters, and less than 24 characters)
6. Head to the Pipelines Library tab, and add a new Secure File named 'google-creds.json' containing your google SA credentials
7. Upon first build, you will need to authorize resource use of the Secure File (build will fail, but will grant you option to authorize)