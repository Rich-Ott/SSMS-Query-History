using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSMSQueryHistory
{
    /// <summary>
    /// Class to track history entries
    /// </summary>
    public class HistoryEntry
    {
        /// <summary>
        /// Stores the server name
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Stores the database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Stores the DateTime of the query
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Stores the query text
        /// </summary>
        public string Query { get; set; }
    }
}
