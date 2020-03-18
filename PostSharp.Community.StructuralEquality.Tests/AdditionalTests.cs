namespace PostSharp.Community.StructuralEquality.Tests
{
    public class AdditionalTests
    {
        
    }

    public class AdditionalObject
    {
        private int? Hello;

        public int GetIt()
        {
            int a = Hello == null ? 0 : Hello.GetHashCode();
            return a;
        }
    }
}