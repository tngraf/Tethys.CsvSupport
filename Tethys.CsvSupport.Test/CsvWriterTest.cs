// ---------------------------------------------------------------------------
// <copyright file="CsvWriterTest.cs" company="Tethys">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CsvWriterTest
    {
        private Stream GetStreamFromString(string text)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;
            return stream;

        }

        [TestMethod]
        public void TestWriteHeaderSimple()
        {
            //var data = new List<string>
            var header = new List<string>
            {
                "1", "2", "3",
            };
            using var stream = new MemoryStream();
            Assert.IsNotNull(stream);
            var sut = new CsvWriter(string.Empty, null, header);
            sut.Write(stream);
            var binary = stream.GetBuffer();
            var raw = Encoding.UTF8.GetString(binary);
            Assert.IsFalse(string.IsNullOrEmpty(raw));
            var csv = raw[0..raw.IndexOf('\0')];
            Assert.IsFalse(string.IsNullOrEmpty(csv));
            Assert.AreEqual("1;2;3\r\n", csv);
        }

        [TestMethod]
        public void TestWrite1()
        {
            var data = new List<List<string>>();
            data.Add(new List<string>
            {
                "A",
                "BSD 2-Clause \"Simplified\" License",
                "CCC",
            });
            
            using var stream = new MemoryStream();
            Assert.IsNotNull(stream);
            var sut = new CsvWriter(string.Empty, data, null);
            sut.Write(stream);
            var binary = stream.GetBuffer();
            var raw = Encoding.UTF8.GetString(binary);
            Assert.IsFalse(string.IsNullOrEmpty(raw));
            var csv = raw[0..raw.IndexOf('\0')];
            Assert.IsFalse(string.IsNullOrEmpty(csv));
            Assert.AreEqual("A;\"BSD 2-Clause \"\"Simplified\"\" License\";CCC\r\n", csv);
        }
    }
}