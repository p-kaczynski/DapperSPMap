using System;
using System.Collections.Generic;
using System.Linq;

namespace DapperSPMap
{
    internal class DynamicGroupingKey<T> : IEquatable<DynamicGroupingKey<T>>
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public DynamicGroupingKey(Dictionary<string,Func<T, object>> selectors, T obj)
        {
            foreach (var name in selectors.Keys)
            {
                var selector = selectors[name];
                _values.Add(name, selector(obj));
            }
        }

        public Dictionary<string, object> Values
        {
            get { return _values; }
        }

        public override bool Equals(object obj)
        {
            var otherKeyObj = obj as DynamicGroupingKey<T>;
            return otherKeyObj != null && Equals(otherKeyObj);
        }

        public bool Equals(DynamicGroupingKey<T> other)
        {
            return ReferenceEquals(this, other) || Values.Keys.All(
                key => other.Values.ContainsKey(key) && Values[key].Equals(other.Values[key]));
        }

        public override int GetHashCode()
        {
            // Modified Bernstein as LINQ expression
            return Values.SelectMany(kvp => kvp.Key + (kvp.Value ?? string.Empty).ToString())
                .Aggregate(0, (current, c) => 33*current ^ c);
        }
    }
}