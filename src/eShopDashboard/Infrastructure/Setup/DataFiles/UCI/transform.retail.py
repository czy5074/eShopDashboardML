
# coding: utf-8

# In[1]:


import pandas as pd
from pandas.tseries.offsets import *


# # Transform process
# Transforms retail.csv to catalog, orders and orderItems for eShopDashboard

# In[2]:

print ("Reading input retail.csv")

retail_csv = pd.read_csv('retail.csv')
retail_csv['date'] = pd.to_datetime(retail_csv.InvoiceDate)


# remove last month-period, because it only contains one week of data

# In[3]:

print ("Processing...")

retail = retail_csv.copy()

retail.date = retail.date.apply(lambda x: x + DateOffset(years=6))
retail = retail[~((retail.date.dt.year == 2017) & (retail.date.dt.month == 12))]


# In[4]:


retail = retail.dropna()


# In[5]:


retail = retail[retail.Quantity>0]


# In[6]:


countries = retail.groupby("Country").size()
countries = countries[countries > 244]
countries.drop('Channel Islands', inplace=True)
countries.head()


# In[7]:


retail = retail[retail.Country.isin(countries.index)]
retail.Country = retail.Country.apply(lambda x: "United States" if (x == "Belgium") else x)


# In[8]:


#retail.InvoiceNo = retail.InvoiceNo.astype('category')
#retail.InvoiceNo = retail.InvoiceNo.apply(lambda x: x.cat.codes)
retail.InvoiceNo = pd.Categorical(retail.InvoiceNo).codes + 1001


# In[9]:


retail.Description = retail.Description.str.replace(",","-")
retail.Description = retail.Description.str.replace('"','')


# In[10]:


retail = retail.groupby(['StockCode','Country','CustomerID','InvoiceNo']).agg({'Quantity' : 'sum', 'UnitPrice' : 'max', 'date': 'max', 'Description' : 'first'}).reset_index()




# In[13]:


products = retail.sort_values(['date']).groupby(['StockCode']).agg({'Description': 'first', 'UnitPrice':'mean'}).reset_index()
products.rename(columns={'UnitPrice':'Price'}, inplace=True)
products = products.assign(Name = products.Description, AvailableStock = 4, MaxStockThreshold = 5, OnReorder = 0, RestockThreshold = 1, CatalogBrandId=5, CatalogTypeId = 5)
products['Id'] = range(100, products.shape[0]+100)


# In[37]:


orders = retail[['InvoiceNo','Country','date']].groupby(['InvoiceNo']).agg({'date':'max','Country':'max'}).reset_index()
orders['Description']=""
orders.rename(columns={'InvoiceNo':'Id','Country':'Address_Country'}, inplace=True)
orders['OrderDate'] = orders.date.apply(lambda x: pd.datetime.strftime(x,'%Y-%m-%dT%H:%M:%S'))


# In[38]:


orderItems = retail[['InvoiceNo','StockCode','Quantity','date','Country']].merge(products, left_on='StockCode', right_on='StockCode', how='inner')
orderItems.rename(columns={'Id':'ProductId', 'InvoiceNo':'OrderId','Description':'ProductName','Quantity':'Units','Price':'UnitPrice'}, inplace=True)
orderItems['Id']=range(1000, retail.shape[0]+1000)
orderItems = orderItems[['Id','OrderId','ProductId','ProductName','UnitPrice','Units']]



# In[40]:

print ("Writing input database files")

products[['Id','CatalogBrandId','CatalogTypeId','Name','Description','Price','AvailableStock','MaxStockThreshold','OnReorder','RestockThreshold']].to_csv('catalog.csv',index=False)
orders[['Id','Address_Country','OrderDate','Description']].to_csv('orders.csv',index=False)
orderItems[['Id','OrderId','ProductId','UnitPrice','Units','ProductName']].to_csv('orderItems.csv',index=False)


