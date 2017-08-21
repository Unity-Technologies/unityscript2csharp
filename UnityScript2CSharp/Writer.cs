using System;
using System.Text;

namespace UnityScript2CSharp
{
    internal class Writer
    {
        private StringBuilder _builder;
        private int _indentation;
        private string _toBeWrittenBeforeNextNewLine;
        private static readonly string _newLine = Environment.NewLine;

        public Writer(string contents)
        {
            _builder = new StringBuilder(contents);
        }

        public bool IndentNextWrite { get; set; }

        public string Text { get { return _builder.ToString();  } }

        public int Identation
        {
            get { return _indentation; }
            set
            {
                _indentation = value;
                CurrentIdentation = new String(' ', _indentation * 4);
            }
        }

        public void Write(string str)
        {
            IndentIfRequired();
            _builder.Append(str);
        }

        public void Write(char ch)
        {
            IndentIfRequired();
            _builder.Append(ch);
        }

        internal void Write(long l)
        {
            IndentIfRequired();
            _builder.Append(l);
        }

        public void WriteLine()
        {
            if (_toBeWrittenBeforeNextNewLine != null)
            {
                _builder.Append(_toBeWrittenBeforeNextNewLine);
                _toBeWrittenBeforeNextNewLine = null;
            }

            _builder.Append(_newLine);
            IndentNextWrite = true;
        }

        public void WriteLine(string str)
        {
            Write(str);
            WriteLine();
        }
        public void WriteBeforeNextNewLine(string text)
        {
            _toBeWrittenBeforeNextNewLine = text;
        }

        public static string NewLine {  get { return _newLine; } }
        
        private void IndentIfRequired()
        {
            if (IndentNextWrite)
            {
                _builder.Append(CurrentIdentation);
                IndentNextWrite = false;
            }
        }

        private string CurrentIdentation { get; set; }
    }
}
