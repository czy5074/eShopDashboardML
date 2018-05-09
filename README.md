# eShopDashboardML
eShopDashboardML is a simple ASP.NET Core app with Product Sales Forecast predictions using Machine Learning.NET.

This sample app is a monolithic ASP.NET Core Razor app and it's main focus is to highlight the usage of Machine Learning .NET API by showing how to train, create and evaluate/predict models related to Product Sales Forecast prediction.

The app is also using a SQL Server database for regular product catalog and orders info, as many typical ASP.NET Core apps using SQL Server. In this case, since it is an example, it is by default using a localdb SQL database so there's no need to setup a real SQL Server. The localdb database will be created with sample populated data the first time you run the app.
If you want to use a real SQL Server or Azure SQL Database, you just need to change the connection string in the app.

Here's a sample screenshot of one of the forecast predictions:

![image](./docs/images/eShopDashboard.png)

## Walkthroughs and setting it up

Check the Wiki to learn how to set it up in Visual Studio and further explanations on the code:

https://github.com/dotnet-architecture/eShopDashboardML/wiki

For further info about how to get started, check the additional info at the [docs](./docs/README.md) folder.

## Citation
eShopDashboardML dataset is based on a public Online Retail Dataset from **UCI**: http://archive.ics.uci.edu/ml/datasets/online+retail
> Daqing Chen, Sai Liang Sain, and Kun Guo, Data mining for the online retail industry: A case study of RFM model-based customer segmentation using data mining, Journal of Database Marketing and Customer Strategy Management, Vol. 19, No. 3, pp. 197â€“208, 2012 (Published online before print: 27 August 2012. doi: 10.1057/dbm.2012.17).


