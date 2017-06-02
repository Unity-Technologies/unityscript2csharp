using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        public SourceFile[] SingleSourceFor(string fileName, string contents)
        {
            return new[] { new SourceFile { FileName = fileName, Contents = contents } };
        }
    }
}
