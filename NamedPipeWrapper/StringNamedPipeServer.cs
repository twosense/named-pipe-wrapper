using System.IO.Pipes;

namespace NamedPipeWrapper
{
    public class StringNamedPipeServer : NamedPipeServer<string>
    {
        public StringNamedPipeServer(string pipeName) : base(pipeName)
        {
        }

        public StringNamedPipeServer(string pipeName, int bufferSize, PipeSecurity security) : base(pipeName,
            bufferSize, security)
        {
        }
    }
}