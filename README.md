Data Debugging
==============

Testing and static analysis can help root out bugs in programs, but not in data. We introduce _data debugging_, an approach that combines program analysis and statistical analysis to _automatically_ find potential data errors. Since it is impossible to know a priori whether data are erroneous, data debugging instead locates data that has a disproportionate impact on the computation. Such data is either very important or wrong. Data debugging is especially useful in the context of data-intensive programming environments that intertwine data with programs in the form of queries or formulas.

CheckCell is an implementation of data debugging for Excel spreadsheets.  CheckCell highlights suspected errors in red, one cell at a time.  After inspecting a cell, the user can correct the data or marked the cell as 'OK'. CheckCell is efficient; its algorithms are asymptotically optimal, and the current prototype runs in seconds for most spreadsheets.

Try CheckCell:
==============

You will need Microsoft Excel 2010 or 2013 and Windows 7 or newer.

Installing CheckCell
--------------------
Download the CheckCell installer in the "Releases" section of the CheckCell GitHub page.  Double-click on the installer to install.  The installer should install all prerequisites for you.

You will find CheckCell installed in Excel, under the "Add-Ins" tab in the ribbon.

Getting CheckCell Source
------------------------
CheckCell depends on an Excel parsing library called "Parcel", also available on GitHub.  Parcel is a git submodule for CheckCell.  This means that you should recursively clone the CheckCell repository if you plan to work the source:

```
git clone --recursive https://github.com/plasma-umass/DataDebug
```

You will need Visual Studio 2013 in order to build the CheckCell plugin.

Using CheckCell
---------------
To use CheckCell, open the spreadsheet you would like to audit, and click CheckCell's "Analyze" button. (It is located in the "Add-Ins" tab in the ribbon.) CheckCell will perform its analysis, and if any potential errors are found, they will be highlighted one at a time in decreasing order of importance. For each highlighted cell, you will have to decide if it is actually an error. If so, click on the "Fix Error" button and enter the correct value in the box that comes up. Otherwise, click the "Mark as OK" button. After each correction, CheckCell will re-run its analysis using the corrected value.

CheckCell's sensitivity level is adjustable. By default it is set to display the top 5% most unusual values, but you may change it by entering a different value in the box labeled "% Most Unusual to Show".  Note that CheckCell may report that no values warrant special attention.

Uninstalling CheckCell
----------------------
CheckCell can be uninstalled in the Windows Add/Remove Programs dialog.