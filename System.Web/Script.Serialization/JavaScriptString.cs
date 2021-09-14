namespace System.Web.Script.Serialization
{
    using System;

    internal class JavaScriptString
    {
        private int _index;
        private string _s;

        internal JavaScriptString(string s)
        {
            this._s = s;
        }

        internal string GetDebugString(string message)
        {
            object[] objArray1 = new object[] { message, " (", this._index, "): ", this._s };
            return string.Concat(objArray1);
        }

        internal char? GetNextNonEmptyChar()
        {
            while (this._s.Length > this._index)
            {
                int num = this._index;
                this._index = num + 1;
                char c = this._s[num];
                if (!char.IsWhiteSpace(c))
                {
                    return new char?(c);
                }
            }
            return null;
        }

        internal int IndexOf(string substr)
        {
            if (this._s.Length > this._index)
            {
                return (this._s.IndexOf(substr, this._index, StringComparison.CurrentCulture) - this._index);
            }
            return -1;
        }

        internal char? MoveNext()
        {
            if (this._s.Length > this._index)
            {
                int num = this._index;
                this._index++;
                return new char?(this._s[num]);
            }
            return null;
        }

        internal string MoveNext(int count)
        {
            if (this._s.Length >= (this._index + count))
            {
                int num = this._index;
                this._index += count;
                return this._s.Substring(num, count);
            }
            return null;
        }

        internal void MovePrev()
        {
            if (this._index > 0)
            {
                this._index--;
            }
        }

        internal void MovePrev(int count)
        {
            while ((this._index > 0) && (count > 0))
            {
                this._index--;
                count--;
            }
        }

        internal string Substring(int length)
        {
            if (this._s.Length > (this._index + length))
            {
                return this._s.Substring(this._index, length);
            }
            return this.ToString();
        }

        public override string ToString()
        {
            if (this._s.Length > this._index)
            {
                return this._s.Substring(this._index);
            }
            return string.Empty;
        }
    }
}

