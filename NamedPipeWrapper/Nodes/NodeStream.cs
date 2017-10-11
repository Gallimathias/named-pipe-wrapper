using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Nodes
{
    public class NodeStream : Stream
    {
        private MemoryMappedViewStream viewStream;

        public NodeStream(MemoryMappedViewStream viewStream)
        {
            this.viewStream = viewStream;
        }

        public override bool CanRead => viewStream.CanRead;

        public override bool CanSeek => viewStream.CanSeek;

        public override bool CanWrite => viewStream.CanWrite;

        public override long Length => viewStream.Length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
