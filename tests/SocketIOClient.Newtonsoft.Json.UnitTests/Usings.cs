global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using FluentAssertions;

[assembly: Parallelize(Workers = 8, Scope = ExecutionScope.ClassLevel)]