# StubDatabaseUtility

## Summary

c# utiltity for setting up and tearing down DB's for use in integration and acceptance testing

## Introduction

Using the stub database utility, stub databases can be created and loaded up with test data for 
running integration or acceptance tests against. The utility will create a copy of a database without 
any data in it. The utility provides two methods which should be used when using created stub 
databases. 

* CopyDatabase should be used once at the start of each test run. This method creates a script which creates all tables, stored procs, functions and indexes from the source database. The method then drops and recreates the destination database to remove any created database from previous test runs and create a fresh version based on the source server (an integration server for the appropriate release branch).
* ClearTables should be run in the setup of each test. It goes through each table in the test database and clears down any created test data.

## Usage

See the tests [here](NHSChoices.StubDatabaseUtility.Tests.Integration/StubDatabaseTests.cs) for example usage.

The app config in a project which is using test database created with this utility will need to be updated 
to contain connection strings which point the code to the empty test database. This connection string should 
point to the same server which is supplied in the destination server connection string.

In NUnit, the CopyDatabase function can be called in a class tagged with the SetUpFixture 
attribute containing a method tagged with the SetUp attribute. This SetUp method will be called once 
at the beginning of each test run for tests within the same namespace as the setup fixture class.

If using SpecFlow then the binding attribute BeforeTestRun will achieve the same result.

Before each test is run the ClearTables function should be called followed by any scripts to 
create seeded data . This can be done in NUnit using a SetUp method in a master class which all tests 
inherit from. Specflow can achieve this using the BeforeScenario attribute.
