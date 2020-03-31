namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.OverridenWithIgnoredProperties
{
    [StructuralEquality]
    public class ProjectBaseClass
    {
        public virtual string Location { get; set; }

        public int X { get; set; }

        public static bool operator ==(ProjectBaseClass left, ProjectBaseClass right) => Operator.Weave(left, right);
        public static bool operator !=(ProjectBaseClass left, ProjectBaseClass right) => Operator.Weave(left, right);
    }
}