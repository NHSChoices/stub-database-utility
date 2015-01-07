Introduction

Using the stub database utility, stub databases can be created and loaded up with test data for 
running integration or acceptance tests against. The utility will create a copy of a database without 
any data in it. The utility provides two methods which should be used when using created stub 
databases. 

	- CopyDatabase should be used once at the start of each test run. This method creates 
	  a script which creates all tables, stored procs, functions and indexes from the source database.
	  The method then drops and recreates the destination database to remove any created database from 
	  previous test runs and create a fresh version based on the source server (an integration server 
	  for the appropriate release branch).
	- ClearTables - This function should be run in the setup of each test. It goes through each 
	  table in the test database and clears down any created test data.

Parameters

The two functions take an objects of type SqlConnection as input. The properties of this object
contain the information required when performing the two functions above.
	- destinationServerConnection is the connection to the DB containing the test database which will be created.
	  This can be for the sql server on your VM eg. VHCDEVSRVXXX\SQL2008R2 or SQL Express on your host
	  machine eg. HCDEVPCXXX\SQLEXPRESS.
	- sourceServerConnection is the connection to the DB containing the database to take a copy of. This should be
	  the empty integration DB for the relevant release. This is only required when using the 
	  DropAndRecreateDatabase function.

Usage

The app config in a project which is using test database created with this utility will need to 
contain connection strings to point the code to the empty test database. This connection string should 
point to the same server which is supplied in the destination server connection string.

In NUnit, the CopyDatabase function can be called in a class tagged with the SetUpFixture 
attribute containing a method tagged with the SetUp attribute. This SetUp method will be called once 
at the beginning of each test run for tests within the same namespace as the setup fixture class.

If using SpecFlow then the binding attribute BeforeTestRun will achieve the same result.

Before each test is run the ClearTables function should be called followed by and scripts to 
create seeded data . This can be done in NUnit using a SetUp method in a master class which all tests 
inherit from. Specflow can achieve this using the BeforeScenario attribute.