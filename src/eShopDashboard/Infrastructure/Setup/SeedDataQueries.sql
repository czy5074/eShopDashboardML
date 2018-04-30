/*
* Use these queries to generate seed data files from origin database
*/

/* Typical mssql extension connection settings ([Ctrl]+[,])
"mssql.connections": [
    {
        "server": "10.0.75.1,5433",
        "database": "Microsoft.eShopOnContainers.Services.CatalogDb",
        "authenticationType": "SqlLogin",
        "user": "sa",
        "password": "Pass@word",
        "emptyPasswordInput": false,
        "savePassword": true,
        "profileName": "eShopOnContainers.Catalog"
    },
    {
        "server": "10.0.75.1,5433",
        "database": "Microsoft.eShopOnContainers.Services.OrderingDb",
        "authenticationType": "SqlLogin",
        "user": "sa",
        "password": "Pass@word",
        "emptyPasswordInput": false,
        "savePassword": true,
        "profileName": "eShopOnContainers.Ordering"
    }
]
*/

-- Orders.csv
select 
    Id, 
    quotename(Address_Country, '''') Address_Country, 
    '''' + Description + '''' Description, 
    quotename(OrderDate, '''') OrderDate
from Ordering.Orders

-- OrderItems.csv
select 
    Id, 
    OrderId, 
    ProductId, 
    UnitPrice, 
    Units, 
    quotename(ProductName, '''') ProductName
from Ordering.OrderItems

