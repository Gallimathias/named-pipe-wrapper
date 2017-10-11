using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Nodes
{
    struct Tile
    {
        public byte Address { get; private set; }
        public bool Owner { get; private set; }
        public long Size { get; private set; }

        public Tile(byte address, bool owner, long size)
        {
            Address = address;
            Owner = owner;
            Size = size;
        }

        

    }
}
