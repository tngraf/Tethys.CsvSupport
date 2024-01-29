// ---------------------------------------------------------------------------
// <copyright file="CsvReaderTest.cs" company="Tethys">
//   Copyright (C) 2021-2024 T. Graf
// </copyright>
//
// Licensed under the Apache License, Version 2.0.
// SPDX-License-Identifier: Apache-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
// either express or implied.
// ---------------------------------------------------------------------------

namespace Tethys.CsvSupport.Test
{
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CsvReaderTest
    {
        private static Stream GetStreamFromString(string text)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;
            return stream;

        }

        [TestMethod]
        public void TestReadHeaderSimple()
        {
            const string Header = "1;2;3";
            var stream = GetStreamFromString(Header);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = true;
            sut.Read(stream);
            Assert.IsNotNull(sut.Headers);
            Assert.AreEqual(3, sut.Headers.Count);
            Assert.AreEqual("1", sut.Headers[0]);
            Assert.AreEqual("2", sut.Headers[1]);
            Assert.AreEqual("3", sut.Headers[2]);
        }

        [TestMethod]
        public void TestReadDataSimple()
        {
            const string Csv = "1;2;3\r\nA;BB;CCC";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = true;
            sut.Read(stream);
            Assert.IsNotNull(sut.Headers);
            Assert.AreEqual(3, sut.Headers.Count);
            Assert.AreEqual("1", sut.Headers[0]);
            Assert.AreEqual("2", sut.Headers[1]);
            Assert.AreEqual("3", sut.Headers[2]);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("A", sut.Result[0][0]);
            Assert.AreEqual("BB", sut.Result[0][1]);
            Assert.AreEqual("CCC", sut.Result[0][2]);
        }

        [TestMethod]
        public void TestReadEscape1()
        {
            const string Header = "1;\"2 2\";3";
            var stream = GetStreamFromString(Header);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = true;
            sut.Read(stream);
            Assert.IsNotNull(sut.Headers);
            Assert.AreEqual(3, sut.Headers.Count);
            Assert.AreEqual("1", sut.Headers[0]);
            Assert.AreEqual("2 2", sut.Headers[1]);
            Assert.AreEqual("3", sut.Headers[2]);
        }

        [TestMethod]
        public void TestReadEscape2()
        {
            const string Csv = "1;2;3\r\n\"A A\";\"B\r\nB\";\"C\"\"C\"\"C\"";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = true;
            sut.Read(stream);
            Assert.IsNotNull(sut.Headers);
            Assert.AreEqual(3, sut.Headers.Count);
            Assert.AreEqual("1", sut.Headers[0]);
            Assert.AreEqual("2", sut.Headers[1]);
            Assert.AreEqual("3", sut.Headers[2]);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("A A", sut.Result[0][0]);
            Assert.AreEqual("B\r\nB", sut.Result[0][1]);
            Assert.AreEqual("C\"C\"C", sut.Result[0][2]);
        }

        [TestMethod]
        public void TestReadEmptyLinesInQuote()
        {
            const string Csv = "A;\"B\r\n\r\nB\";CCC";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = false;
            sut.ExpectedFieldCount = 3;
            sut.Read(stream);
            Assert.IsNull(sut.Headers);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("A", sut.Result[0][0]);
            Assert.AreEqual("B\r\n\r\nB", sut.Result[0][1]);
            Assert.AreEqual("CCC", sut.Result[0][2]);
        }

        [TestMethod]
        public void TestReadHeaderInQuotes()
        {
            const string Csv = "A;\"BSD 2-Clause \"\"Simplified\"\" License\";CCC";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = false;
            sut.ExpectedFieldCount = 3;
            sut.Read(stream);
            Assert.IsNull(sut.Headers);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("A", sut.Result[0][0]);
            Assert.AreEqual("BSD 2-Clause \"Simplified\" License", sut.Result[0][1]);
            Assert.AreEqual("CCC", sut.Result[0][2]);
        }

        [TestMethod]
        public void TestRoundtrip1()
        {
            const string Csv = "BSD-2-Clause;\"BSD 2-Clause \"\"Simplified\"\" License\";BSD-2-Clause";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = false;
            sut.ExpectedFieldCount = 3;
            sut.Read(stream);
            Assert.IsNull(sut.Headers);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("BSD-2-Clause", sut.Result[0][0]);
            Assert.AreEqual("BSD 2-Clause \"Simplified\" License", sut.Result[0][1]);
            Assert.AreEqual("BSD-2-Clause", sut.Result[0][2]);

            using var stream2 = new MemoryStream();
            Assert.IsNotNull(stream2);
            var sut2 = new CsvWriter(string.Empty, sut.Result, null);
            sut2.Write(stream2);
            var binary = stream2.GetBuffer();
            var raw = Encoding.UTF8.GetString(binary);
            Assert.IsFalse(string.IsNullOrEmpty(raw));
            var csv = raw[0..raw.IndexOf('\0')];
            Assert.IsFalse(string.IsNullOrEmpty(csv));
            Assert.AreEqual("BSD-2-Clause;\"BSD 2-Clause \"\"Simplified\"\" License\";BSD-2-Clause\r\n", csv);
        }

        [TestMethod]
        public void TestRoundtrip2()
        {
            const string Csv = "BSD-2-Clause,\"BSD 2-Clause \"\"Simplified\"\" License\",BSD-2-Clause";
            var stream = GetStreamFromString(Csv);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = false;
            sut.Separator = ',';
            sut.ExpectedFieldCount = 3;
            sut.Read(stream);
            Assert.IsNull(sut.Headers);

            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Result.Count);
            Assert.AreEqual(3, sut.Result[0].Count);
            Assert.AreEqual("BSD-2-Clause", sut.Result[0][0]);
            Assert.AreEqual("BSD 2-Clause \"Simplified\" License", sut.Result[0][1]);
            Assert.AreEqual("BSD-2-Clause", sut.Result[0][2]);

            using var stream2 = new MemoryStream();
            Assert.IsNotNull(stream2);
            var sut2 = new CsvWriter(string.Empty, sut.Result, null);
            sut2.Separator = ',';
            sut2.Write(stream2);
            var binary = stream2.GetBuffer();
            var raw = Encoding.UTF8.GetString(binary);
            Assert.IsFalse(string.IsNullOrEmpty(raw));
            var csv = raw[0..raw.IndexOf('\0')];
            Assert.IsFalse(string.IsNullOrEmpty(csv));
            Assert.AreEqual("BSD-2-Clause,\"BSD 2-Clause \"\"Simplified\"\" License\",BSD-2-Clause\r\n", csv);
        }

        [TestMethod]
        public void TestReadDoubleQuotes()
        {
            const string Header = "1;\"\";3";
            var stream = GetStreamFromString(Header);
            var sut = new CsvReader(string.Empty);
            sut.UseHeader = true;
            sut.Read(stream);
            Assert.IsNotNull(sut.Headers);
            Assert.AreEqual(3, sut.Headers.Count);
            Assert.AreEqual("1", sut.Headers[0]);
            Assert.AreEqual("", sut.Headers[1]);
            Assert.AreEqual("3", sut.Headers[2]);
        }
    }
}