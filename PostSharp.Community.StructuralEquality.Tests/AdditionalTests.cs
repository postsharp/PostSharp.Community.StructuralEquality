namespace PostSharp.Community.StructuralEquality.Tests
{
    public class AdditionalTests
    {
        
    }

    public class AdditionalObject
    {
#pragma warning disable 649
        private int? Hello;
#pragma warning restore 649

        public int GetIt()
        {
            int a = Hello == null ? 0 : Hello.GetHashCode();
            return a;
        }
    }
}