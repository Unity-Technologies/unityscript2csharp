using System;
using System.Text;

namespace UnityScript2CSharp
{
    internal class Writer
    {
        private StringBuilder _builder;
        private int _indentation;
        private int _checkPoint;
        private static readonly string _newLine = Environment.NewLine;

        public Writer(string contents)
        {
            _builder = new StringBuilder(contents);
            _checkPoint = 0;
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
            _checkPoint = _builder.Length;
            IndentIfRequired();
            _builder.Append(str);
        }

        public void Write(char ch)
        {
            _checkPoint = _builder.Length;
            IndentIfRequired();
            _builder.Append(ch);
        }

        internal void Write(long l)
        {
            _checkPoint = _builder.Length;
            IndentIfRequired();
            _builder.Append(l);
        }

        public void WriteLine()
        {
            _checkPoint = _builder.Length;
            _builder.Append(_newLine);
            IndentNextWrite = true;
        }

        public void WriteLine(string str)
        {
            Write(str);
            WriteLine();
        }

        public static string NewLine {  get { return _newLine; } }

        public void DiscardLastWrittenText()
        {
            _builder.Remove(_checkPoint, _builder.Length - _checkPoint);
            _checkPoint = _builder.Length;
        }

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
