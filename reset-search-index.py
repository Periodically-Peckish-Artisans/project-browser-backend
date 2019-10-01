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

def resetDataSource(dataSourceName, container):
  executeSearchFunc('DELETE', '/datasources/%s' % (dataSourceName), {})
  executeSearchFunc('POST', '/datasources', {
      'name' : dataSourceName,
      'type' : 'azureblob',
      'credentials' : { 'connectionString' : storageconnstr },  
      'container' : { 'name' : container },  
  })

resetDataSource('projectdatasource', 'project')
resetDataSource('eventdatasource', 'event')

def resetIndex(indexFileName):
  indexConfig = {}
  with open('%s.json' % (indexFileName)) as fp:
    indexConfig = json.load(fp)
  indexName = indexConfig['name']
  executeSearchFunc('DELETE', '/indexes/%s' % (indexName), {})
  executeSearchFunc('POST', '/indexes', indexConfig)

def resetIndexer(indexerFileName):
  indexerConfig = {}
  with open('%s.json' % (indexerFileName)) as fp:
    indexerConfig = json.load(fp)
  indexerName = indexerConfig['name']
  executeSearchFunc('DELETE', '/indexers/%s' % (indexerName), {})
  executeSearchFunc('POST', '/indexers', indexerConfig)

resetIndex('project-search-index')
resetIndexer('project-search-indexer')

resetIndex('event-search-index')
resetIndexer('event-search-indexer')