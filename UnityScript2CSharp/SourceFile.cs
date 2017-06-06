namespace UnityScript2CSharp
{
    public struct SourceFile
    {
        public string FileName;
        public string Contents;

        public SourceFile(string fileName, string contents)
        {
            FileName = fileName;
            Contents = contents;
        }
    }
}
