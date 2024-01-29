// ---------------------------------------------------------------------------
// <copyright file="CsvReader.cs" company="Tethys">
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

// Links
// * https://www.loc.gov/preservation/digital/formats/fdd/fdd000323.shtml
// * http://super-csv.github.io/super-csv/csv_specification.html
namespace Tethys.CsvSupport
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Tethys.Logging;

    /// <summary>
    /// A simple reader for CSV files.
    /// </summary>
    public class CsvReader
    {
        #region PRIVATE PROPERTIES
        /// <summary>
        /// The logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(CsvReader));

        /// <summary>
        /// The headers.
        /// </summary>
        private List<string> headers;

        /// <summary>
        /// The result.
        /// </summary>
        private List<List<string>> result;

        /// <summary>
        /// The line counter.
        /// </summary>
        private int lineCounter;
        #endregion // PRIVATE PROPERTIES

        //// ---------------------------------------------------------------------

        #region PUBLIC PROPERTIES
        /// <summary>
        /// The field separator.
        /// </summary>
        public const char DefaultSeparator = ';';

        /// <summary>
        /// The default quote character.
        /// </summary>
        public const char DefaultQuoteChar = '"';

        /// <summary>
        /// Gets the filename.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets or sets the separator.
        /// </summary>
        public char Separator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use header.
        /// </summary>
        public bool UseHeader { get; set; }

        /// <summary>
        /// Gest or sets the expected field count.
        /// </summary>
        public int ExpectedFieldCount { get; set; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        public IReadOnlyList<string> Headers => this.headers;

        /// <summary>
        /// Gets the result.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Result => this.result;
        #endregion // PUBLIC PROPERTIES

        //// ---------------------------------------------------------------------

        #region CONSTRUCTION
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvReader" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public CsvReader(string filename)
        {
            this.Filename = filename;
            this.Separator = DefaultSeparator;
            this.ExpectedFieldCount = 0;
        } // CsvReader()
        #endregion // CONSTRUCTION

        //// ---------------------------------------------------------------------

        #region PUBLIC METHODS
        /// <summary>
        /// Guesses the separator by analyzing the first lines of the CSV file.
        /// </summary>
        /// <returns>A char.</returns>
        public char GuessSeparator()
        {
            using var fs = new FileStream(this.Filename, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                } // if

                var c1 = line.Count(c => c == ';');
                var c2 = line.Count(c => c == ',');
                if (c1 > c2)
                {
                    return ';';
                } // if

                // default
                return ',';
            } // while

            // default
            return ',';
        } // GuessSeparator()

        /// <summary>
        /// Reads the CSV file.
        /// </summary>
        public void Read()
        {
            using var fs = new FileStream(this.Filename, FileMode.Open, FileAccess.Read);
            this.Read(fs);
        } // Read()

        /// <summary>
        /// Reads the CSV file.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        public void Read(Stream fileStream)
        {
            this.lineCounter = 0;

            this.result = new List<List<string>>();

            using var sr = new StreamReader(fileStream);
            var firstLine = true;
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

#if false // DEBUG
                if (line.StartsWith("TAPR-OHL-1.0,"))
                {
                    firstLine = false;
                }
#endif

                var fields = this.ProcessSingleRequestLine(line, sr);

                if (firstLine && this.UseHeader)
                {
                    this.headers = fields;
                    firstLine = false;
                }
                else
                {
                    if (this.UseHeader && (fields.Count != this.headers.Count))
                    {
                        Log.Warn($"Line {this.lineCounter}: Data field count mismatch!");
                        continue;
                    } // if

                    this.result.Add(fields);
                } // if

                this.lineCounter++;
            } // while
        } // Read()
        #endregion // PUBLIC METHODS

        //// ---------------------------------------------------------------------

        #region PRIVATE METHODS

        /// <summary>
        /// Processes the single request line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="sr">The <see cref="StreamReader"/>.</param>
        /// <returns>
        /// A list of string values.
        /// </returns>
        public List<string> ProcessSingleRequestLine(string line, StreamReader sr)
        {
            var fields = new List<string>();
            var pos = 0;
            var quoted = false;
            var start = 0;
            string field;
            var quoteBuffer = new StringBuilder(2000);

            if (line == null)
            {
                return fields;
            } // if

            line = this.ReplaceQuotes(line);

            while ((line != null) && (pos < line.Length))
            {
                if (line[pos] == DefaultQuoteChar)
                {
                    quoted = !quoted;

                    // special case: quote at the end of multi-line field
                    if (pos < line.Length - 1)
                    {
                        pos++;
                        continue;
                    } // if
                } // if

                if (quoted)
                {
                    if (pos == line.Length - 1)
                    {
                        if (fields.Count < this.ExpectedFieldCount)
                        {
                            quoteBuffer.Append(line.Substring(start));
                            quoteBuffer.Append("\r\n");

                            // read another line
                            do
                            {
                                line = sr.ReadLine();
                                if (string.IsNullOrEmpty(line))
                                {
                                    quoteBuffer.Append("\r\n");
                                }
                                else
                                {
                                    line = this.ReplaceQuotes(line);
                                } // if

                                this.lineCounter++;
                            }
                            while (string.IsNullOrEmpty(line));

                            line = this.ReplaceQuotes(line);
                            pos = 0;
                            start = 0;
                        } // if
                    }
                    else
                    {
                        pos++;
                    } // if

                    continue;
                } // if

                if (line[pos] == this.Separator)
                {
                    var end = pos - 1;
                    quoteBuffer.Append(line.Substring(start, (end - start) + 1));
                    field = quoteBuffer.ToString();
                    field = PostProcessField(field);
                    fields.Add(field);
                    start = pos + 1;
                    quoteBuffer.Clear();
                } // if

                pos++;
            } // while

            // add last field
            if (line != null)
            {
                field = line[start..];
                field = PostProcessField(field);
                fields.Add(field);
            } // if

            if (fields.Count > this.ExpectedFieldCount)
            {
                this.ExpectedFieldCount = fields.Count;
            } // if

            return fields;
        } // ProcessSingleRequestLine()

        /// <summary>
        /// Postsprocesses the given field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>The updated field.</returns>
        private static string PostProcessField(string field)
        {
            field = RestoresQuotes(field);
            if ((field.Length > 1) && (field[0] == DefaultQuoteChar) && (field[^1] == DefaultQuoteChar))
            {
                field = field[1..^1];
            } // if

            if (ContainsOnlyQuotes(field))
            {
                field = string.Empty;
            } // if

            return field;
        } // PostProcessField()

        /// <summary>
        /// Determines whether the specified text contains only quotes.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the text contains only quotes; otherwise, <c>false</c>.
        /// </returns>
        private static bool ContainsOnlyQuotes(string text)
        {
            return text.All(c => c == '"');
        } // ContainsOnlyQuotes()

        /// <summary>
        /// Replaces the quotes.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>A cleaned up text string.</returns>
        private string ReplaceQuotes(string text)
        {
            if (text == null)
            {
                return null;
            } // if

            // worst case: """""" -> """
            text = text.Replace("\"\"\"\"\"\"", "☺☺☺");

            // worst case: """" -> ""
            text = text.Replace("\"\"\"\"", "☺☺");

            if (this.Separator == DefaultSeparator)
            {
                // triple quotes at the beginning: ;""" -> ;"'
                text = text.Replace(";\"\"\"", ";\"☺");

                // triple quotes at the end: """; -> '";
                text = text.Replace("\"\"\";", "☺\";");
            }
            else
            {
                // triple quotes at the beginning: ;""" -> ;"'
                text = text.Replace(",\"\"\"", ",\"☺");

                // triple quotes at the end: """; -> '";
                text = text.Replace("\"\"\",", "☺\",");
            } // if

            // double quotes: "" -> '
            text = text.Replace("\"\"", "☺");

            return text;
        } // ReplaceQuotes()

        /// <summary>
        /// Restores the quotes.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>An updated up text string.</returns>
        private static string RestoresQuotes(string text)
        {
            if (text == null)
            {
                return null;
            } // if

            text = text.Replace("☺☺☺", "\"\"\"");
            text = text.Replace("☺☺", "\"\"");
            text = text.Replace("☺", "\"");

            return text;
        } // RestoresQuotes()
        #endregion // PRIVATE METHODS
    } // CsvReader
} // Siemens.SwcDataAnalyzer.Core
