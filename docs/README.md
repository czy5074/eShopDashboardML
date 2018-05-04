# eShopDashboard
eShopDashboard is a simple ASP.NET Core app with Product Sales Forecast predictions using Machine Learning.NET.

This sample app is a monolithic ASP.NET Core Razor app and it's main focus is to highlight the usage of ML.NET API by showing how to train, create and evaluate/predict models related to Product Sales Forecast prediction.

The app is also using a SQL Server database for regular product catalog and orders info, as many typical ASP.NET Core apps using SQL Server. In this case, since it is an example, it is by default using a localdb SQL database so there's no need to setup a real SQL Server. The localdb database will be created with sample populated data the first time you run the app.
If you want to use a real SQL Server or Azure SQL Database, you just need to change the connection string in the app.

Here's a sample screenshot of one of the forecast predictions:

![image](./images/eShopDashboard.png)

## Project Docs
For further info about how to get started, check the additional info at .MD files : 

- [Setting up eShopDashboard in Visual Studio and running it](./Setting-up-eShopDashboard-in-Visual-Studio-and-running-it.md)
- [Create and Train the models](./Create-and-train-the-models-%5BOptional%5D.md) [optional]
- [ML.NET Code Walkthrough](./ML.NET-Code-Walkthrough.md)



