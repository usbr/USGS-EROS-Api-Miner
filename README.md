# USGS-EROS-Api-Miner
C# program in .Net 4.0 to to automate the download of USGS EarthExplorer Data from the https://earthexplorer.usgs.gov/ website. Data is used by the BCOO Water Accounting and Verification Group to build Agricultural datasets derived through crop classification processes using the best available Landsat data. API documentation may be found at this link: https://earthexplorer.usgs.gov/inventory/documentation.

The program is written as a command-line executable so that it can easily be incorporated into an automated run schedule. Source is written in C# and relies on the third-party software libraries Newtonsoft-JSON, SharpZipLib, and RestSharp.

The program will search for LANDSAT8 and LANDSAT7 GeoTIFF products that are within the input Path & Row boundaries between input dates t1 & t2. The program will either dump the metadata for the found files in a CSV file, or download the files into a directory and unpack its contents.

Sample Usage:
>`ErosMiner --user=XXXXX --pass=XXXXX --inventory=MyFile --download=C:\Temp\ --paths=38,39 --rows=36,37 --t1=2016-01-01 --t2=2016-01-10`

Inputs:
>
user: USGS Earth Explorer user name  
pass: USGS Earth Explorer password  
inventory: CSV file name  
download: valid path where logged-in user has write privileges  
paths: integer paths separated by a comma  
rows: integer rows seperated by a comma  
t1: start date in YYYY-MM-DD format  
t2: end date in YYYY-MM-DD format  

This program is distributed with an MIT license.
