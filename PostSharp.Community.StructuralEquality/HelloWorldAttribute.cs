using PostSharp.Extensibility;

namespace PostSharp.Community.DeepSerializable
{
    /// <summary>
    /// The code <c>Console.WriteLine("Hello, world!");</c> is injected at the beginning of each target method.
    /// </summary>
    [MulticastAttributeUsage(MulticastTargets.Method)]
     // Only methods are targets of this attribute. If this attribute is applied to a class or assembly, it means
     // that it targets all methods in that class or assembly.
    [RequirePostSharp("PostSharp.Community.StructuralEquality.Weaver", "HelloWorldTask")]
     // When PostSharp runs on an assembly, if at least one if its declarations is affected by this attribute,
     // then PostSharp will search for an assembly named PostSharp.Community.StructuralEquality.Weaver.dll and look for 
     // an exported task named HelloWorldTask in that assembly, and that task will be executed during weaving.
    public class HelloWorldAttribute : MulticastAttribute
    { 
    }
}