using Open.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShipwreckLib
{
    public class PortRange
    {
        public static PortRange Parse(string value)
        {
            int index = value.IndexOf(Environment.NewLine);
            if (index == -1) return new PortRange(0);
            int length = int.Parse(value.Substring(0, index));
            var result = new PortRange(length);
            if (length == 0)
                return result;
            length--;
            for (int i = 0; i < length; i++)
            {
                int next = value.IndexOf(Environment.NewLine, index += Environment.NewLine.Length);
                result.Values[i] = PortSpan.Parse(value.Substring(index, next - index));
                index = next;
            }
            result.Values[length] = PortSpan.Parse(value.Substring(
                index += Environment.NewLine.Length,
                value.Length - index - Environment.NewLine.Length));
            result.Count = result.Values.Length;
            return result;
        }

        public static PortRange From(IEnumerable<Mapping> mappings, out int customscount)
        {
            customscount = 0;
            var result = new PortRange(mappings.Count());
            foreach (var mapping in mappings)
            {
                var cur = new Port(mapping);
                if (cur.Custom) customscount++;
                result.Add(cur);
            }
            return result;
        }

        protected PortSpan[] Values = new PortSpan[0];
        public int Length
        {
            get => Values.Length;
            set
            {
                var values = new PortSpan[value];
                Count = Math.Min(values.Length, Count);
                for (int i = 0; i < Count; i++)
                    values[i] = Values[i];
                Values = values;
            }
        }
        public int Count { get; protected set; }
        public PortSpan this[int index] => Values[index];
        PortRange(PortSpan[] Values)
        {
            this.Values = Values;
            Count = Values.Length;
        }
        public PortRange(int Length = 0) : this(new PortSpan[Length])
        {
            Count = 0;
        }

        public static void CopyTo(PortRange values, PortRange to)
        {
            int length = Math.Min(values.Length, to.Length);
            CopyTo(values.Values, to.Values, length);
            to.Count = length;
        }
        public static void CopyTo(PortSpan[] values, PortSpan[] to, int length)
        {
            for (int i = 0; i < length; i++)
                to[i] = PortSpan.Copy(values[i]);
        }

        public virtual void Add(Port value)
        {
            Add(new PortSpan(value));
        }
        public virtual void Add(PortSpan value)
        {
            int lastindex = -1, end = Count;
            var cur = value;
            while (++lastindex < end)
            {
                var last = Values[lastindex];
                if (last.Contains(cur))
                {
                    last.Length++;
                    end = lastindex + last.Length;
                }
                else if (last.Overlaps(cur))
                {
                    cur.Length += last.Length + 1;
                    cur.Stretch(last);
                    int curend = lastindex + cur.Length;
                    for (int i = lastindex + last.Length + 1; i < end; i += last.Length + 1)
                    {
                        last = Values[i];
                        if (last.Overlaps(cur))
                        {
                            cur.Stretch(last);
                            var arr = new PortSpan[last.Length + 1];
                            arr[0] = last;
                            for (int x = 1; x < arr.Length; x++) arr[x] = Values[i + x];
                            for (int x = lastindex; x > curend; x--) Values[x + arr.Length] = Values[x];
                            for (int x = 0; x < arr.Length; x++) Values[curend + x] = arr[x];
                            curend += arr.Length;
                            cur.Length += arr.Length;
                        }
                    }
                    break;
                }
                else lastindex += last.Length;
            }
            for (int x = Count; x > lastindex;) Values[x] = Values[--x];
            Values[lastindex] = cur;
            Count++;
        }

        public int Overlaps(IPort value)
        {
            for(int i = 0; i < Count;)
            {
                var cur = Values[i];
                if (cur.Overlaps(value)) return i;
                i += cur.Length + 1;
            }
            return -1;
        }

        public override string ToString()
        {
            var result = new StringBuilder(Length.ToString() + Environment.NewLine);
            for (int i = 0; i < Values.Length; i++)
            {
                var current = Values[i];
                result.AppendLine(current == null ? "null" : current.ToString());
            }
            return result.ToString();
        }

        public PortRange Remove(int index)
        {
            if (index < 0 || index >= Count) 
                throw new IndexOutOfRangeException("No such index found in port");
            var result = new PortRange(Count - 1);
            for (int i = 0; i < index; i++)
                result.Add(new PortSpan(this[i].Port));
            for (int i = index + 1; i < Count; i++)
                result.Add(new PortSpan(this[i].Port));
            return result;
        }

        public Port? Find(Func<Port, bool> matchs)
        {
            var index = FindIndex(matchs);
            if (index == -1)
                return null;
            return this[index].Port;
        }

        public int FindIndex(Func<Port, bool> matchs)
        {
            for (int i = 0; i < Count; i++)
            {
                var port = this[i].Port;
                if (matchs.Invoke(port)) return i;
            }
            return -1;
        }
    }
}
