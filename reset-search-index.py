# Currently, this will make the site search functions not work until the search indexes are rebuilt.
# If you wanted to get fancy, you could slot it into another index rather than deleting the main one.

import json
import http.client
import os

def executeSearchFunc(method, relativeUrl, body):
  key = json.loads(os.environ['SEARCHKEYS'])['primaryKey']
  baseurl = '%s.search.windows.net' % (os.environ['APPNAME'])

  req = http.client.HTTPSConnection(baseurl)
  req.request(method, relativeUrl + '?api-version=2019-05-06', body = json.dumps(body), 
    headers = {
      'api-key': key,
      'Content-Type': 'application/json'
  })
  response = req.getresponse()
  print('Http request:')
  print('%s %s' % (method, relativeUrl))
  print(response.status)
  print(response.read())
  print()
  req.close()

storageconnstr = json.loads(os.environ['STORAGECONNSTR'])['connectionString']

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