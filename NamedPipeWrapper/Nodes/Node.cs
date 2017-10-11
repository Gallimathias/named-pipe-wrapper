using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Nodes
{
    public class Node : IDisposable
    {
        private MemoryMappedFile memory;
        private Tile[] tiles;
        private long size;
        private byte count;

        public Node(string name, long size, byte count)
        {
            memory = MemoryMappedFile.CreateOrOpen(name, size);
            tiles = new Tile[count];
            this.size = size - count;
            this.count = count;
        }

        public void Read(byte address)
        {
            using (var stream = memory.CreateViewStream(
                count + address * tiles[address].Size,
                tiles[address].Size,
                MemoryMappedFileAccess.Read))
            {
                
            }
        }

        public void Write(byte address)
        {

        }


        public void Dispose()
        {
            memory?.Dispose();

            memory = null;

            GC.SuppressFinalize(this);
        }

        private void InitNodes()
        {
            for (byte i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new Tile(i, true, size / count);
            }
        }

        ~Node()
        {
            Dispose();
        }
    }
}
