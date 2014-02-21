﻿
//Original code from Simple and fast CSV library in C# by By Pascal Ganaye
//http://www.codeproject.com/Articles/685310/Simple-and-fast-CSV-library-in-Csharp

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace FileStorage
{
    internal class CsvReader<T> : IEnumerable<T>, IEnumerator<T> where T : new()
    {
        private readonly Dictionary<Type, List<Action<T, String>>> allSetters = new Dictionary<Type, List<Action<T, String>>>();
        private static DateTime DateTimeZero = new DateTime();
        private string[] columns;
        private char curChar;
        private int len;
        private string line;
        private int pos;
        private T record;
        private readonly char fieldSeparator;
        private readonly StreamReader streamReader;
        private readonly char textQualifier;
        private readonly StringBuilder parseFieldResult = new StringBuilder();


        public CsvReader(StreamReader reader, char fieldSeparator= ';', char textQualifier = '"')
        {
            this.fieldSeparator = fieldSeparator;
            this.textQualifier = textQualifier;

            this.streamReader = reader;

            this.ReadNextLine();

            var readColumns = new List<string>();
            string columnName;

            while ((columnName = this.ParseField()) != null)
            {
                readColumns.Add(columnName);
                if (this.curChar == this.fieldSeparator)
                    this.NextChar();
                else
                    break;
            }
            this.columns = readColumns.ToArray();

        }

        public T Current
        {
            get { return this.record; }
        }


        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        public void Dispose()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            this.ReadNextLine();
            if (this.line == null && (this.line = this.streamReader.ReadLine()) == null)
            {
                this.record = default(T);
            }
            else
            {
                this.record = new T();
                Type recordType = typeof(T);
                List<Action<T, String>> setters;
                if (!this.allSetters.TryGetValue(recordType, out setters))
                {
                    var list = new List<Action<T, string>>();
                    for (int i = 0; i < this.columns.Length; i++)
                    {
                        string columnName = this.columns[i];
                        Action<T, string> action = null;
                        if (columnName.IndexOf(' ') >= 0)
                            columnName = columnName.Replace(" ", "");
                        action = FindSetter(columnName, false) ?? FindSetter(columnName, true);

                        list.Add(action);
                    }
                    setters = list;
                    this.allSetters[recordType] = setters;
                }

                var fieldValues = new string[setters.Count];
                for (int i = 0; i < setters.Count; i++)
                {
                    fieldValues[i] = this.ParseField();
                    if (this.curChar == this.fieldSeparator)
                        this.NextChar();
                    else
                        break;
                }
                for (int i = 0; i < setters.Count; i++)
                {
                    var setter = setters[i];
                    if (setter != null)
                    {
                        setter(this.record, fieldValues[i]);
                    }
                }
            }
            return (this.record != null);
        }


        public void Reset()
        {
            throw new NotImplementedException("Cannot reset CsvFileReader enumeration.");
        }

        private static Action<T, string> EmitSetValueAction(MemberInfo mi, Func<string, object> func)
        {
            ParameterExpression paramExpObj = Expression.Parameter(typeof(object), "obj");
            ParameterExpression paramExpT = Expression.Parameter(typeof(T), "instance");

            var pi = mi as PropertyInfo;
            if (pi != null)
            {
                return (Action<T, string>)((o, v) => pi.SetValue(o, func(v), null));
            }

            var fi = mi as FieldInfo;
            if (fi != null)
            {
                //ParameterExpression valueExp = Expression.Parameter(typeof(string), "value");
                var valueExp = Expression.ConvertChecked((Expression)paramExpObj, (Type)fi.FieldType);

                // Expression.Property can be used here as well
                MemberExpression fieldExp = Expression.Field(paramExpT, fi);
                BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);

                var setter = Expression.Lambda<Action<T, object>>
                    (assignExp, paramExpT, paramExpObj).Compile();

                return (o, s) => setter(o, func(s));
            }
            throw new NotImplementedException();
        }

        private static Action<T, string> FindSetter(string c, bool staticMember)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | (staticMember ? BindingFlags.Static : BindingFlags.Instance);
            Action<T, string> action = null;
            PropertyInfo pi = typeof(T).GetProperty(c, flags);
            if (pi != null)
            {
                var pFunc = StringToObject(pi.PropertyType);
                action = EmitSetValueAction(pi, pFunc);
            }
            FieldInfo fi = typeof(T).GetField(c, flags);
            if (fi != null)
            {
                var fFunc = StringToObject(fi.FieldType);
                action = EmitSetValueAction(fi, fFunc);
            }
            return action;
        }

        private static Func<string, object> StringToObject(Type propertyType)
        {
            if (propertyType == typeof(string))
                return (s) => s ?? String.Empty;
            else if (propertyType == typeof(Int32))
                return (s) => String.IsNullOrEmpty(s) ? 0 : Int32.Parse(s);
            if (propertyType == typeof(DateTime))
                return (s) => String.IsNullOrEmpty(s) ? DateTimeZero : DateTime.Parse(s);
            else
                throw new NotImplementedException();
        }


        private void NextChar()
        {
            if (this.pos < this.len)
            {
                this.pos++;
                this.curChar = this.pos < this.len ? this.line[this.pos] : '\0';
            }
        }

        private void ParseEndOfLine()
        {
            throw new NotImplementedException();
        }


        private string ParseField()
        {
            parseFieldResult.Length = 0;
            if (this.line == null || this.pos >= this.len)
                return null;
            while (this.curChar == ' ' || this.curChar == '\t')
            {
                this.NextChar();
            }
            if (this.curChar == this.textQualifier)
            {
                this.NextChar();
                while (this.curChar != 0)
                {
                    if (this.curChar == this.textQualifier)
                    {
                        this.NextChar();
                        if (this.curChar == this.textQualifier)
                        {
                            this.NextChar();
                            parseFieldResult.Append(this.textQualifier);
                        }
                        else
                            return parseFieldResult.ToString();
                    }
                    else if (this.curChar == '\0')
                    {
                        if (this.line == null)
                            return parseFieldResult.ToString();
                        this.ReadNextLine();
                    }
                    else
                    {
                        parseFieldResult.Append(this.curChar);
                        this.NextChar();
                    }
                }
            }
            else
            {
                while (this.curChar != 0 && this.curChar != this.fieldSeparator && this.curChar != '\r' && this.curChar != '\n')
                {
                    parseFieldResult.Append(this.curChar);
                    this.NextChar();
                }
            }
            return parseFieldResult.ToString();
        }

        private void ReadNextLine()
        {
            this.line = this.streamReader.ReadLine();
            this.pos = -1;
            if (this.line == null)
            {
                this.len = 0;
                this.curChar = '\0';
            }
            else
            {
                this.len = this.line.Length;
                this.NextChar();
            }
        }
    }
}
// full source end