eShopDashboardML dataset is based on a public Online Retail Dataset from UCI: http://archive.ics.uci.edu/ml/datasets/online+retail

eShopDashboardML uses a generic schema database; nevetheless, a custom process was built to transform original input dataset to the database schema required.

In order to generate the input database files, execute the `transform.retail.py` python script (python environment needs pandas to be installed before)

```python
python transform.retail.py
```

After executing the script, you should copy the generated files (`catalog.csv`, `orders.csv` and `orderItems.csv`) to the folder `src\eShopDashboard\Infrastructure\Setup\DataFiles`. 