namespace System.Web.Util
{
    using System;
    using System.Globalization;

    internal class HashCodeCombiner
    {
        private long _combinedHash;

        internal HashCodeCombiner()
        {
            this._combinedHash = 0x1505L;
        }

        internal HashCodeCombiner(long initialCombinedHash)
        {
            this._combinedHash = initialCombinedHash;
        }

        internal void AddArray(string[] a)
        {
            if (a != null)
            {
                int length = a.Length;
                for (int i = 0; i < length; i++)
                {
                    this.AddObject(a[i]);
                }
            }
        }

        internal void AddDateTime(DateTime dt)
        {
            this.AddInt(dt.GetHashCode());
        }

        internal void AddInt(int n)
        {
            this._combinedHash = ((this._combinedHash << 5) + this._combinedHash) ^ n;
        }

        internal void AddObject(bool b)
        {
            this.AddInt(b.GetHashCode());
        }

        internal void AddObject(byte b)
        {
            this.AddInt(b.GetHashCode());
        }

        internal void AddObject(int n)
        {
            this.AddInt(n);
        }

        internal void AddObject(long l)
        {
            this.AddInt(l.GetHashCode());
        }

        internal void AddObject(object o)
        {
            if (o != null)
            {
                this.AddInt(o.GetHashCode());
            }
        }
        

        internal long CombinedHash =>
            this._combinedHash;

        internal int CombinedHash32 =>
            this._combinedHash.GetHashCode();

        internal string CombinedHashString =>
            this._combinedHash.ToString("x", CultureInfo.InvariantCulture);
    }
}

