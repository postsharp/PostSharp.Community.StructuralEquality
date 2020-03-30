using System.Collections;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Helper static class used by code synthesized by <see cref="StructuralEqualityAttribute"/>.
    /// </summary>
    public static class CollectionHelper
    {
        /// <summary>
        /// Returns true if both enumerables have the same number of elements and all of those elements are equal, in order.
        /// </summary>
        /// <param name="left">One enumerable.</param>
        /// <param name="right">The second enumerable.</param>
        /// <returns>True if the collections are equal.</returns>
        public static bool Equals( IEnumerable left, IEnumerable right )
        {
            if ( left == null )
            {
                return right == null;
            }
            if ( right == null )
            {
                return false;
            }

            var leftEnumerator = left.GetEnumerator();
            var rightEnumerator = right.GetEnumerator();
            while ( true )
            {
                var leftHasNext = leftEnumerator.MoveNext();
                var rightHasNext = rightEnumerator.MoveNext();

                if ( leftHasNext && rightHasNext && object.Equals(leftEnumerator.Current, rightEnumerator.Current ))
                {
                    continue;
                }
                
                return !leftHasNext && !rightHasNext;
            }
        }
    }
}