using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Nodes
{
    public class Node : IDisposable
    {
        MemoryMappedFile memory;

        public Node(string name, long size)
        {
            memory = MemoryMappedFile.CreateOrOpen(name, size);
        }

        public void Dispose()
        {
            memory?.Dispose();

            memory = null;

            GC.SuppressFinalize(this);
        }

        ~Node()
        {
            Dispose();
        }
    }
}
