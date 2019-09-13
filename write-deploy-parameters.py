import json
import os

obj = {}
obj["$schema"] = "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#"
obj["contentVersion"] = "1.0.0.0"
params = {
  "appName": {
    "value": os.environ['APPNAME']
  },
  "storageAccountType": {
    "value": "Standard_LRS"
  },
  "location": {
    "value": os.environ["AZURELOCATION"]
  },
  "googleServiceAccountCreds": {
    "value": "google-creds.json" # We'll dump this file into the package in the build script
  }
}

obj["parameters"] = params
with open("azuredeploy-parameters.json", "w") as write_file:
    json.dump(obj, write_file)
