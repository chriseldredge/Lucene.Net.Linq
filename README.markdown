## LINQ to Lucene.Net

[![Build Status](https://travis-ci.org/themotleyfool/Lucene.Net.Linq.png?branch=master)](https://travis-ci.org/themotleyfool/Lucene.Net.Linq)

Lucene.Net.Linq is a .net library that enables LINQ queries to run natively on a Lucene.Net index.

* Automatically converts PONOs to Documents and back
* Add, delete and update documents in atomic transaction
* Unit of Work pattern automatically tracks and flushes updated documents
* Update/replace documents with \[Field(Key=true)\] to prevent duplicates
* Term queries
* Prefix queries
* Range queries and numeric range queries
* Complex boolean queries
* Native pagination using Skip and Take
* Support storing and querying NumericField 
* Automatically convert complex types for storing, querying and sorting
* Custom boost functions using IQueryable<T>.Boost() extension method
* Sort by standard string, NumericField or any type that implements IComparable
* Sort by item.Score() extension method to sort by relevance
* Specify custom format for DateTime stored as strings
* Register cache-warming queries to be executed when IndexSearcher is being reloaded

## Available on NuGet Gallery

To install the [Lucene.Net.Linq package](http://nuget.org/packages/Lucene.Net.Linq),
run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

    PM> Install-Package Lucene.Net.Linq

## Examples

1. Using [fluent syntax](source/Lucene.Net.Linq.Tests/Samples/FluentConfiguration.cs) to configure mappings
1. Using [attributes](source/Lucene.Net.Linq.Tests/Samples/AttributeConfiguration.cs) to configure mappings
1. Specifying [document keys](source/Lucene.Net.Linq.Tests/Samples/DocumentKeys.cs)

Upcoming features / ideas / bugs / known issues
-------------------------

See Issues on the GitHub project page.
