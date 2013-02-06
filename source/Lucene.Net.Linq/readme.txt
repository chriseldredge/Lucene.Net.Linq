Thank you for trying Lucene.Net.Linq!

Getting Started
===============

See https://github.com/themotleyfool/Lucene.Net.Linq for examples and documentation.

Upgrading to Version 3.1
========================

Version 3.1 brings new options to make Lucene.Net.Linq simpler to use
for projects that aren't already using Lucene.Net. Where before the
client had to construct its own IndexWriter and Analyzer instances
and pass them into LuceneDataProvider, new constructors are available
that allow you to simply provide a Directory and Version, and it will
use metadata on your objects to build an appropriate Analyzer.

LuceneDataProvider now implements IDisposable. If you allow it
to create the IndexWriter for you, you must call Dispose to ensure
that the writer is properly closed.

3.1 also adds new overloads to methods on LuceneDataProvider that
allow you to provide your own implementation of IDocumentMapper<T>
enabling clients to provide custom solutions for mapping fields
to objects, control how keys are generated for documents and more.

