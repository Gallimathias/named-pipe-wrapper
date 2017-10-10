using System.IO.Pipes;

namespace NamedPipeWrapper
{
    static class PipeServerFactory
    {
        public static NamedPipeServerStream CreateAndConnectPipe(string pipeName, PipeSecurity pipeSecurity)
        {
            var pipe = CreatePipe(pipeName, pipeSecurity);
            pipe.WaitForConnection();
            return pipe;
        }

        public static NamedPipeServerStream CreatePipe(string pipeName, PipeSecurity pipeSecurity)
            => new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                0,
                0,
                pipeSecurity);

    }
}
