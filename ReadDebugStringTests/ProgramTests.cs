using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadDebugString;
using System;
using System.Collections.Generic;

namespace ReadDebugStringTests
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void BuildCommandLine_EmptyEnumerable_ReturnsEmptyString()
        {
            var input = new List<string>();
            var actual = Program.BuildCommandLine(input);
            Assert.AreEqual("", actual);
        }

        [TestMethod]
        [DataRow("", "\"\"")]
        [DataRow("\"", "\"\\\"\"")]
        [DataRow(" \" ", "\" \\\" \"")]
        [DataRow(" \\", "\" \\\\\"")]
        [DataRow(" \\\\ ", "\" \\\\ \"")]
        [DataRow(" \\\" ", "\" \\\\\\\" \"")]
        [DataRow("one two three", "\"one two three\"")]
        [DataRow("one \"two\" three", "\"one \\\"two\\\" three\"")]
        [DataRow("x\\\"x", "\"x\\\\\\\"x\"")]
        public void BuildCommandLine_SingleString_ReturnsEscapedString(string input, string expected)
        {
            var inputList = new List<string>()
            {
                input,
            };
            var actual = Program.BuildCommandLine(inputList);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BuildCommandLine_MultiString_ReturnsJoinedString()
        {
            var input = new List<string>()
            {
                "a",
                "b",
            };
            var actual = Program.BuildCommandLine(input);
            Assert.AreEqual("\"a\" \"b\"", actual);
        }

        [TestMethod]
        public void BuildCommandLine_ModuleNameGiven_ReturnsJoinedString()
        {
            var inputModuleName = "a";
            var inputCommandLine = new List<string>
            {
                "b",
            };
            var actual = Program.BuildCommandLine(inputModuleName, inputCommandLine);
            Assert.AreEqual("\"a\" \"b\"", actual);
        }

        [TestMethod]
        public void BuildCommandLine_ModuleNameContainsDoubleQuote_ThrowsException()
        {
            var inputModuleName = "\"";
            var inputCommandLine = new List<string>();
            _ = Assert.ThrowsException<ArgumentException>(() => Program.BuildCommandLine(inputModuleName, inputCommandLine));
        }
    }
}
