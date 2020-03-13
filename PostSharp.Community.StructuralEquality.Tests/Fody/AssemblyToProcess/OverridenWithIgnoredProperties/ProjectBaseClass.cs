namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.OverridenWithIgnoredProperties
{
    public class ProjectBaseClass
    {
        public virtual string Location { get; set; }

        public int X { get; set; }
    }
}