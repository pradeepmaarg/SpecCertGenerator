using System;
using System.IO;
using System.Collections;


namespace Maarg.AllAboard
{

    public class ConcatenatingStream : Stream
    {
        Stream[] mStreamList;
        int mCurrentStreamIndex;
        long mPosition;

        public ConcatenatingStream(Stream stream1, Stream stream2, Stream stream3)
        {
            if (stream1 == null)
            {
                throw new ArgumentNullException("stream1");
            }

            if (stream2 == null && stream3 != null)
            {
                throw new ArgumentException("stream3");
            }


            if (stream2 == null)
            {
                mStreamList = new Stream[1];
                mStreamList[0] = stream1;
            }

            else if (stream3 == null)
            {
                mStreamList = new Stream[2];
                mStreamList[0] = stream1;
                mStreamList[1] = stream2;
            }

            else
            {
                mStreamList = new Stream[3];
                mStreamList[0] = stream1;
                mStreamList[1] = stream2;
                mStreamList[2] = stream3;
            }

            mPosition = mCurrentStreamIndex = 0;
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytes = mCurrentStreamIndex == mStreamList.Length
                ? 0 : ReadInternal(buffer, offset, count);
            mPosition += bytes;

            return bytes;
        }


        private int ReadInternal(byte[] buffer, int offset, int count)
        {
            int initCount = count;
            int totalReadBytes = 0;
            int bytesRead;

            while (totalReadBytes < initCount)
            {
                bytesRead = mStreamList[mCurrentStreamIndex].Read(buffer, offset, count);
                totalReadBytes += bytesRead;
                offset += bytesRead;
                count -= bytesRead;

                if (bytesRead == 0) //current stream over
                {
                    mCurrentStreamIndex++;
                    if (mCurrentStreamIndex == mStreamList.Length)
                    {
                        break;
                    }
                }
            }

            return totalReadBytes;
        }


        #region General Stream stuff
        override public long Length
        {
            get
            {
                throw new NotSupportedException("Length.get is not supported");
            }
        }

        override public long Position
        {
            get
            {
                return mPosition;
            }

            set
            {
                throw new NotSupportedException("Position.set is not supported");
            }
        }

        override public bool CanRead
        {
            get { return true; }
        }

        override public bool CanSeek
        {
            get { return false; }
        }

        override public void SetLength(long val)
        {
            throw new NotSupportedException("StitchingStream does not support SetLength()");
        }

        override public void Flush()
        {
            throw new NotSupportedException("StitchingStream does not support Flush()");
        }

        override public bool CanWrite
        {
            get { return false; }
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("StitchingStream does not support Write()");
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            long pos = -1;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = offset;
                    break;
                case SeekOrigin.Current:
                    pos = mPosition + offset;
                    break;
                case SeekOrigin.End:
                    break;
            }

            if (pos == mPosition)
            {
                return pos;
            }
            else
            {
                throw new NotSupportedException("StitchingStream does not support Seek()");
            }
        }
        #endregion
    }
}
