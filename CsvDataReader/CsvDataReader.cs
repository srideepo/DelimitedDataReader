﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace SqlUtilities
{
    public class CsvDataReader : IDataReader, IDisposable
    {
        // The DataReader should always be open when returned to the user.
        private bool _isClosed = false;

        private bool _disposed = false;

        private StreamReader _stream;
        private string[] _headers;
        private string[] _currentRow;
        private string _staticValues = "";
        private char _delimiter = ',';

        // This should match strings and strings that
        // have quotes around them and include embedded commas
        private string _CsvRegexPattern = "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
        private Regex _CsvRegex = new Regex("\\x7C(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))", RegexOptions.Compiled);   //hexvalue \x7C is for pipe |
        private string _RegexSpecialCharacters = @"[{(?\-^|*+.$)}]";

        public CsvDataReader(string fileName) : this(fileName, ',')
        {
        }

        public CsvDataReader(string fileName, char delimiter)
        {
            _delimiter = delimiter;
            int value = Convert.ToInt32(_delimiter);
            //escape delimiters in hexdec form, automatically takes care of backslash escape (pipe fails non hex val escape)
            //form "\\x7C(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))"
            _CsvRegex = new Regex($@"\x{value:X}{_CsvRegexPattern}", RegexOptions.Compiled);

            if (!File.Exists(fileName))
                throw new FileNotFoundException();

            _stream = new StreamReader(fileName);
            _headers = _stream.ReadLine().Split(_delimiter);
        }

        public CsvDataReader(string fileName, Dictionary<string, string> staticColumns)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException();

            _stream = new StreamReader(fileName);

            // Get the raw header
            string rawHeader = _stream.ReadLine();

            // Add all the static columns into the header string
            // And all the values into the string we use for reading
            foreach (KeyValuePair<string, string> keyValue in staticColumns)
            {
                rawHeader += string.Format(",{0}", keyValue.Key);
                _staticValues += string.Format(",{0}", keyValue.Value.ToString());
            }
            _headers = rawHeader.Split(_delimiter);

        }

        public bool Read()
        {
            if (_stream == null) return false;
            if (_stream.EndOfStream) return false;

            string rawRow = _stream.ReadLine();

            // Add any static values that we have
            if (_staticValues.Length > 0)
                rawRow += _staticValues;

            _currentRow = _CsvRegex.Split(rawRow);

            // Unfortunately the Regex keeps the quotes around strings.
            // Those have to go.
            // I'm sure there's a better way but this works.
            for (int i = 0; i < _currentRow.Length; i++)
            {
                _currentRow[i] = _currentRow[i].Trim('"');
            }

            // 
            return true;
        }

        public Object GetValue(int i)
        {
            return _currentRow[i];
        }

        public String GetName(int i)
        {
            return _headers[i];
        }

        public int FieldCount
        {
            // Return the count of the number of columns, which in
            // this case is the size of the column metadata
            // array.
            get { return _headers.Length; }
        }

        ///*
        // * Because the user should not be able to directly create a 
        // * DataReader object, the constructors are
        // * marked as internal.
        // */
        //internal TemplateDataReader(SampleDb.SampleDbResultSet resultset)
        //{
        //    m_resultset = resultset;
        //}

        //internal TemplateDataReader(SampleDb.SampleDbResultSet resultset, TemplateConnection connection)
        //{
        //    m_resultset = resultset;
        //    m_connection = connection;
        //}

        /****
         * METHODS / PROPERTIES FROM IDataReader.
         ****/
        public int Depth
        {
            /*
             * Always return a value of zero if nesting is not supported.
             */
            get { return 0; }
        }

        public bool IsClosed
        {
            /*
             * Keep track of the reader state - some methods should be
             * disallowed if the reader is closed.
             */
            get { return _isClosed; }
        }

        public int RecordsAffected
        {
            /*
             * RecordsAffected is only applicable to batch statements
             * that include inserts/updates/deletes. The sample always
             * returns -1.
             */
            get { return -1; }
        }

        public void Close()
        { 
            _isClosed = true;
        }

        public bool NextResult()
        {
            // The sample only returns a single resultset. However,
            // DbDataAdapter expects NextResult to return a value.
            return false;
        }

        

        public DataTable GetSchemaTable()
        {
            //$
            throw new NotSupportedException();
        }

        /****
         * METHODS / PROPERTIES FROM IDataRecord.
         ****/




        public String GetDataTypeName(int i)
        {
            /*
             * Usually this would return the name of the type
             * as used on the back end, for example 'smallint' or 'varchar'.
             * The sample returns the simple name of the .NET Framework type.
             */
            return "X";
        }

        public Type GetFieldType(int i)
        {
            // Return the actual Type class for the data type.
            return typeof(int);
        }



        public int GetValues(object[] values)
        {
            int i = 0; //, j = 0;
            //for (; i < values.Length && j < m_resultset.metaData.Length; i++, j++)
            //{
            //    values[i] = m_resultset.data[m_nPos, j];
            //}

            return i;
        }

        public int GetOrdinal(string name)
        {
            // Look for the ordinal of the column with the same name and return it.
            //for (int i = 0; i < m_resultset.metaData.Length; i++)
            //{
            //    if (0 == _cultureAwareCompare(name, m_resultset.metaData[i].name))
            //    {
            //        return i;
            //    }
            //}

            int result = -1;
            for (int i = 0; i < _headers.Length; i++)
                if (_headers[i].ToLower() == name.ToLower())
                {
                    result = i;
                    return result;
                }

            // Throw an exception if the ordinal cannot be found.
            string s = string.Format("The column {0} could not be found in the results", name);
            throw new IndexOutOfRangeException(s);
            
        }

        public object this[int i]
        {
            get { return "X"; }
        }

        public object this[String name]
        {
            // Look up the ordinal and return 
            // the value at that position.
            get { return this[GetOrdinal(name)]; }
        }

       

        public Int32 GetInt32(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }






        /*
         * Implementation specific methods.
         */
        //private int _cultureAwareCompare(string strA, string strB)
        //{
        //    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
        //}

        public void Dispose()
        {
            // Based on https://msdn.microsoft.com/en-us/library/fs2xkftw(v=vs.110).aspx
            this.Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _stream.Dispose();

            _disposed = true;

        }

        #region Not Used

        public bool GetBoolean(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }
        public byte GetByte(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            // The sample does not support this method.
            throw new NotSupportedException("GetBytes not supported.");
        }

        public char GetChar(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            // The sample does not support this method.
            throw new NotSupportedException("GetChars not supported.");
        }

        public Guid GetGuid(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public Int16 GetInt16(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotSupportedException();
        }

        public Int64 GetInt64(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public float GetFloat(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public double GetDouble(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public String GetString(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public Decimal GetDecimal(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotSupportedException();
        }

        public DateTime GetDateTime(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
            */
            throw new NotSupportedException();
        }

        public IDataReader GetData(int i)
        {
            /*
             * The sample code does not support this method. Normally,
             * this would be used to expose nested tables and
             * other hierarchical data.
             */
            throw new NotSupportedException("GetData not supported.");
        }


        #endregion


    }
}
