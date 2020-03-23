using System.Collections;

namespace PostSharp.Community.StructuralEquality
{
    public static class CollectionHelper
    {
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

                if ( leftHasNext && rightHasNext && leftEnumerator.Current.Equals( rightEnumerator.Current ) )
                {
                    continue;
                }
                
                return !leftHasNext && !rightHasNext;
            }
        }
    }
}