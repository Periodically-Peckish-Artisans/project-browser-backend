# Currently, this will make the site search functions not work until the search indexes are rebuilt.
# If you wanted to get fancy, you could slot it into another index rather than deleting the main one.

import json
import http.client
import os

searchkey = ''
with open('searchkeys.json') as fp:
  searchkey = json.load(fp)['primaryKey']

def executeSearchFunc(method, relativeUrl, body):
  baseurl = '%s.search.windows.net' % (os.environ['APPNAME'])

  req = http.client.HTTPSConnection(baseurl)
  req.request(method, relativeUrl + '?api-version=2019-05-06', body = json.dumps(body), 
    headers = {
      'api-key': searchkey,
      'Content-Type': 'application/json'
  })
  response = req.getresponse()
  print('Http request:')
  print('%s %s' % (method, relativeUrl))
  print(response.status)
  print(response.read())
  print()
  req.close()

storageconnstr = ''
with open('storagekeys.json') as fp:
  storageconnstr = json.load(fp)['connectionString']

executeSearchFunc('DELETE', '/datasources/projectdatasource', {})
executeSearchFunc('POST', '/datasources', {
    'name' : 'projectdatasource',
    'type' : 'azureblob',
    'credentials' : { 'connectionString' : storageconnstr },  
    'container' : { 'name' : 'project' },  
})

projectSearchIndex = {}
with open('project-search-index.json') as fp:
  projectSearchIndex = json.load(fp)
projName = projectSearchIndex['name']
executeSearchFunc('DELETE', '/indexes/%s' % (projName), {})
executeSearchFunc('POST', '/indexes', projectSearchIndex)

projectSeearchIndexer = {}
with open('project-search-indexer.json') as fp:
  projectSeearchIndexer = json.load(fp)
projIndexerName = projectSeearchIndexer['name']
executeSearchFunc('DELETE', '/indexers/%s' % (projIndexerName), {})
executeSearchFunc('POST', '/indexers', projectSeearchIndexer)