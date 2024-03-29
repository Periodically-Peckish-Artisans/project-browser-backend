# Currently, this will make the site search functions not work until the search indexes are rebuilt.
# If you wanted to get fancy, you could slot it into another index rather than deleting the main one.

import json
import http.client
import os

def executeCmd(cmd):
  return os.popen(cmd).read()

print('Fetching Environment.')
an = os.environ['APPNAME']
rg = os.environ['AZURERESOURCEGROUPNAME']

print('Finding Query Keys.')

keys = json.loads(executeCmd('az search query-key list --resource-group %s --service-name %s' % (rg, an)))

matchingKeys = [x["key"] for i, x in enumerate(keys) if x["name"] == "backendservice"]

print('Found %d key(s) matching our backend service. Deleting.' % (len(matchingKeys)))

for key in matchingKeys:
  executeCmd('az search query-key delete --key-value %s --resource-group %s --service-name %s' % (key, rg, an))

print('Creating new key.')
newQueryKey = json.loads(executeCmd('az search query-key create --name backendservice --resource-group %s --service-name %s' % (rg, an)))


print('Finding admin keys.')

adminKeys = json.loads(executeCmd('az search admin-key show --resource-group %s --service-name %s' % (rg, an)))

print('Setting app settings.')
print(executeCmd('az functionapp config appsettings set --name %s --resource-group %s --settings "AzureSearchServiceAdminApiKey=%s" "AzureSearchServiceQueryApiKey=%s"' % (an, rg, adminKeys["primaryKey"], newQueryKey["key"])))