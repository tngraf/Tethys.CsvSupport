// ---------------------------------------------------------------------------
// <copyright file="CsvWriter.cs" company="Tethys">
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
// * https://csvlint.io/
namespace Tethys.CsvSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Tethys.Logging;

    /// <summary>
    /// A simple writer for CSV files.
    /// </summary>
    public class CsvWriter
    {
        #region PRIVATE PROPERTIES
        /// <summary>
        /// The logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(CsvWriter));
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
        /// Gets the headers.
        /// </summary>
        public IReadOnlyList<string> Headers { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Data { get; }
        #endregion // PUBLIC PROPERTIES

        //// ---------------------------------------------------------------------

        #region CONSTRUCTION
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvWriter"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="data">The data.</param>
        /// <param name="headers">The headers.</param>
        public CsvWriter(string filename, IReadOnlyList<IReadOnlyList<string>> data, IReadOnlyList<string> headers)
        {
            this.Filename = filename;
            this.Data = data;
            this.Headers = headers;
            this.Separator = DefaultSeparator;
        } // CsvWriter()
        #endregion // CONSTRUCTION

        //// ---------------------------------------------------------------------

        #region PUBLIC METHODS
        /// <summary>
        /// Writes the CSV file.
        /// </summary>
        public void Write()
        {
            using var fs = new FileStream(this.Filename, FileMode.Create);
            this.Write(fs);
        } // Write()

        /// <summary>
        /// Writes the CSV file.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        public void Write(Stream fileStream)
        {
            using var sw = new StreamWriter(fileStream);
            try
            {
                if (this.Headers?.Count > 0)
                {
                    for (var i = 0; i < this.Headers.Count; i++)
                    {
                        sw.Write(this.EscapeIfNeeded(this.Headers[i]));
                        if (i < this.Headers.Count - 1)
                        {
                            sw.Write(this.Separator);
                        }
                        else
                        {
                            sw.Write(string.Empty);
                        } // if
                    } // foreach

                    sw.Write("\r\n");
                } // if

                if (this.Data == null)
                {
                    return;
                } // if

                foreach (var csvLine in this.Data)
                {
                    for (var i = 0; i < csvLine.Count; i++)
                    {
                        sw.Write(this.EscapeIfNeeded(csvLine[i]));
                        if (i < csvLine.Count - 1)
                        {
                            sw.Write(this.Separator);
                        }
                        else
                        {
                            sw.Write(string.Empty);
                        } // if
                    } // for

                    sw.Write("\r\n");
                } // foreach
            }
            catch (Exception ex)
            {
                Log.Error("Error writing CSV file: " + ex.Message);
            } // catch
        } // Write()
        #endregion // PUBLIC METHODS

        //// ---------------------------------------------------------------------

        #region PRIVATE METHODS
        /// <summary>
        /// Escapes/unescapes the specified text if needed.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The text.</returns>
        private string EscapeIfNeeded(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            } // if

            if (text.Contains('\"') || text.Contains(' ') || text.Contains('\n') || text.Contains(this.Separator))
            {
                text = this.Escape(text);
            } // if

            return text;
        } // EscapeIfNeeded()

        /// <summary>
        /// Escapes/unescapes the specified text according to CSV rules.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The text.</returns>
        private string Escape(string text)
        {
            // first replace single quotes by double-quotes
            text = text.Replace("\"", "\"\"");

            // have enclosing quotes if needed
            if (text.Contains('\"') || text.Contains(' ') || text.Contains('\n') || text.Contains(this.Separator))
            {
                text = "\"" + text + "\"";
            } // if

            return text;
        } // Escape()
        #endregion // PRIVATE METHODS
    } // CsvWriter
}
