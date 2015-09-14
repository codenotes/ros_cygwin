using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace ROSInstaller
{
    class FullReadAdapter : Stream
    {
        Stream _BaseStream;

        public FullReadAdapter(Stream stream)
        {
            _BaseStream = stream;
        }

        public override bool CanRead
        {
            get
            {
                return _BaseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _BaseStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _BaseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return _BaseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _BaseStream.Position;
            }

            set
            {
                _BaseStream.Position = value;
            }
        }

        public override void Flush()
        {
            _BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int done = _BaseStream.Read(buffer, offset + total, count - total);
                if (done < 0)
                    return done;
                total += done;
                if (done == 0)
                    break;
            }
            return total;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _BaseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _BaseStream.Dispose();
        }

        public override void Close()
        {
            _BaseStream.Close();
        }
    }
}
